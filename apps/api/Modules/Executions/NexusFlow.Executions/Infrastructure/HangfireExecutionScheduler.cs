using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using NexusFlow.Executions.Abstractions;
using NexusFlow.Executions.Application;

namespace NexusFlow.Executions.Infrastructure;

internal sealed class HangfireExecutionScheduler(
    IBackgroundJobClient jobs,
    IServiceScopeFactory scopes) : IExecutionScheduler
{
    public async Task<Guid> EnqueueAsync(
        Guid workflowId,
        TriggerSource source,
        string? triggerPayloadJson,
        CancellationToken ct = default)
    {
        Guid executionId;
        await using (var scope = scopes.CreateAsyncScope())
        {
            var workflows = scope.ServiceProvider.GetRequiredService<NexusFlow.Workflows.Abstractions.IWorkflowReader>();
            var executions = scope.ServiceProvider.GetRequiredService<ExecutionsService>();

            var definition = await workflows.GetDefinitionAsync(workflowId, ct).ConfigureAwait(false);
            if (definition is null)
                throw new InvalidOperationException($"Workflow {workflowId} not found.");

            executionId = await executions
                .CreatePendingExecutionAsync(workflowId, definition.UserId, source, triggerPayloadJson, ct)
                .ConfigureAwait(false);
        }

        jobs.Enqueue<WorkflowExecutor>(x => x.RunAsync(executionId, CancellationToken.None));
        return executionId;
    }
}
