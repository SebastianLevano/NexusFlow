using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NexusFlow.Executions.Abstractions;
using NexusFlow.Executions.Application.Actions;
using NexusFlow.Executions.Application.Templating;
using NexusFlow.Executions.Domain;
using NexusFlow.Executions.Infrastructure;
using NexusFlow.Shared.Time;
using NexusFlow.Workflows.Abstractions;

namespace NexusFlow.Executions.Application;

public sealed class WorkflowExecutor(
    ExecutionsDbContext db,
    IWorkflowReader workflows,
    ActionHandlerResolver actions,
    IExecutionLiveBus liveBus,
    IClock clock,
    ILogger<WorkflowExecutor> logger)
{
    public async Task RunAsync(Guid executionId, CancellationToken ct)
    {
        var execution = await db.Executions
            .Include(e => e.Steps)
            .SingleOrDefaultAsync(e => e.Id == executionId, ct)
            .ConfigureAwait(false);

        if (execution is null)
        {
            logger.LogWarning("Execution {ExecutionId} not found.", executionId);
            return;
        }

        if (execution.Status != ExecutionStatus.Pending)
        {
            logger.LogInformation("Execution {ExecutionId} already in status {Status}; skipping.", executionId, execution.Status);
            return;
        }

        var definition = await workflows.GetDefinitionAsync(execution.WorkflowId, ct).ConfigureAwait(false);
        if (definition is null)
        {
            execution.Fail(clock.UtcNow, "Workflow definition not found.");
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            await PublishExecutionAsync(execution, ct).ConfigureAwait(false);
            return;
        }

        execution.Start(clock.UtcNow);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await PublishExecutionAsync(execution, ct).ConfigureAwait(false);

        var context = new WorkflowExecutionContext(Templater.ParseOrEmpty(execution.TriggerPayload));

        try
        {
            await ExecuteStepsAsync(execution, definition, context, ct).ConfigureAwait(false);
            execution.Succeed(clock.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Execution {ExecutionId} failed.", executionId);
            execution.Fail(clock.UtcNow, Truncate(ex.Message, 2000));
        }
        finally
        {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            await PublishExecutionAsync(execution, ct).ConfigureAwait(false);
        }
    }

    private async Task ExecuteStepsAsync(
        Execution execution,
        WorkflowDefinition definition,
        WorkflowExecutionContext context,
        CancellationToken ct)
    {
        var stepNumber = 0;
        foreach (var stepDef in definition.Steps.OrderBy(s => s.OrderIndex))
        {
            stepNumber++;
            var step = execution.AddStep(stepDef.Id, stepDef.OrderIndex, stepDef.ActionType, clock.UtcNow);
            db.StepExecutions.Add(step);

            JsonElement configElement;
            try
            {
                configElement = Templater.ParseOrEmpty(stepDef.ConfigJson);
            }
            catch (Exception ex)
            {
                step.Fail($"Invalid step config: {ex.Message}", null, clock.UtcNow);
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
                await PublishStepAsync(execution.Id, step, ct).ConfigureAwait(false);
                throw;
            }

            var resolvedConfig = Templater.ResolveJson(configElement, context);
            step.Start(resolvedConfig.GetRawText(), clock.UtcNow);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            await PublishStepAsync(execution.Id, step, ct).ConfigureAwait(false);

            try
            {
                var handler = actions.Resolve(stepDef.ActionType);
                var result = await handler.ExecuteAsync(
                    new ActionInvocation(execution.Id, execution.WorkflowId, execution.UserId, resolvedConfig, context),
                    ct).ConfigureAwait(false);

                step.Succeed(result.Output.GetRawText(), clock.UtcNow);
                context.SetStepOutput(stepNumber, result.Output);
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
                await PublishStepAsync(execution.Id, step, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Step {StepNumber} ({ActionType}) failed for execution {ExecutionId}.",
                    stepNumber, stepDef.ActionType, execution.Id);
                step.Fail(Truncate(ex.Message, 4000), null, clock.UtcNow);
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
                await PublishStepAsync(execution.Id, step, ct).ConfigureAwait(false);
                throw;
            }
        }
    }

    private Task PublishExecutionAsync(Execution execution, CancellationToken ct) =>
        liveBus.PublishExecutionAsync(
            new ExecutionLiveEvent(
                execution.Id,
                execution.Status.ToString().ToLowerInvariant(),
                clock.UtcNow,
                execution.DurationMs,
                execution.ErrorMessage),
            ct);

    private Task PublishStepAsync(Guid executionId, StepExecution step, CancellationToken ct) =>
        liveBus.PublishStepAsync(
            new StepLiveEvent(
                executionId,
                step.Id,
                step.OrderIndex,
                step.ActionType,
                step.Status.ToString().ToLowerInvariant(),
                clock.UtcNow,
                step.DurationMs,
                step.Error,
                step.Status == StepStatus.Running ? step.Input : null,
                step.Status == StepStatus.Succeeded || step.Status == StepStatus.Failed ? step.Output : null),
            ct);

    private static string Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= max ? value : value[..max];
    }
}
