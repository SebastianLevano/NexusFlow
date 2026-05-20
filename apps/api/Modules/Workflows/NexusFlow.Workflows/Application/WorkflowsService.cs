using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NexusFlow.Auth.Abstractions;
using NexusFlow.Executions.Abstractions;
using NexusFlow.Shared.Results;
using NexusFlow.Shared.Time;
using NexusFlow.Workflows.Domain;
using NexusFlow.Workflows.Infrastructure;

namespace NexusFlow.Workflows.Application;

public sealed class WorkflowsService(
    WorkflowsDbContext db,
    ICurrentUser currentUser,
    IScheduleRegistrar scheduleRegistrar,
    IClock clock)
{
    public async Task<Result<IReadOnlyList<WorkflowSummary>>> ListAsync(CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId) return WorkflowErrors.Unauthorized;

        var rows = await db.Workflows
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new
            {
                w.Id,
                w.Name,
                w.Description,
                w.TriggerType,
                w.IsActive,
                StepCount = w.Steps.Count,
                w.CreatedAt,
                w.UpdatedAt,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        IReadOnlyList<WorkflowSummary> summaries = rows
            .Select(r => new WorkflowSummary(
                r.Id, r.Name, r.Description,
                TypeMapping.FormatTrigger(r.TriggerType),
                r.IsActive, r.StepCount, r.CreatedAt, r.UpdatedAt))
            .ToList();

        return Result.Success(summaries);
    }

    public async Task<Result<WorkflowResponse>> GetAsync(Guid id, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId) return WorkflowErrors.Unauthorized;

        var workflow = await db.Workflows
            .AsNoTracking()
            .Include(w => w.Steps.OrderBy(s => s.OrderIndex))
            .SingleOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct)
            .ConfigureAwait(false);

        if (workflow is null) return WorkflowErrors.NotFound;
        return Map(workflow);
    }

    public async Task<Result<WorkflowResponse>> CreateAsync(CreateWorkflowRequest req, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId) return WorkflowErrors.Unauthorized;

        if (!TriggerTypes.All.Contains(req.TriggerType))
            return WorkflowErrors.InvalidTriggerType(req.TriggerType);

        var workflow = Workflow.Create(
            userId,
            req.Name,
            req.Description,
            TypeMapping.ParseTrigger(req.TriggerType),
            SerializeJson(req.TriggerConfig),
            clock.UtcNow);

        var steps = BuildSteps(req.Steps);
        if (steps.IsFailure) return steps.Error;
        workflow.ReplaceSteps(steps.Value);

        db.Workflows.Add(workflow);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return Map(workflow);
    }

    public async Task<Result<WorkflowResponse>> UpdateAsync(Guid id, UpdateWorkflowRequest req, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId) return WorkflowErrors.Unauthorized;

        if (!TriggerTypes.All.Contains(req.TriggerType))
            return WorkflowErrors.InvalidTriggerType(req.TriggerType);

        var workflow = await db.Workflows
            .Include(w => w.Steps)
            .SingleOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct)
            .ConfigureAwait(false);

        if (workflow is null) return WorkflowErrors.NotFound;

        var wasActiveSchedule = workflow.IsActive && workflow.TriggerType == TriggerType.Schedule;

        workflow.Update(
            req.Name,
            req.Description,
            TypeMapping.ParseTrigger(req.TriggerType),
            SerializeJson(req.TriggerConfig),
            clock.UtcNow);

        var steps = BuildSteps(req.Steps);
        if (steps.IsFailure) return steps.Error;
        workflow.ReplaceSteps(steps.Value);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Re-register schedule if trigger config might have changed cron.
        if (workflow.IsActive && workflow.TriggerType == TriggerType.Schedule)
        {
            RegisterSchedule(workflow);
        }
        else if (wasActiveSchedule)
        {
            scheduleRegistrar.RemoveSchedule(workflow.Id);
        }

        return Map(workflow);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId) return WorkflowErrors.Unauthorized;

        var workflow = await db.Workflows
            .SingleOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct)
            .ConfigureAwait(false);

        if (workflow is null) return WorkflowErrors.NotFound;

        db.Workflows.Remove(workflow);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        scheduleRegistrar.RemoveSchedule(id);
        return Result.Success();
    }

    public Task<Result<WorkflowResponse>> ActivateAsync(Guid id, CancellationToken ct)
        => SetActive(id, active: true, ct);

    public Task<Result<WorkflowResponse>> DeactivateAsync(Guid id, CancellationToken ct)
        => SetActive(id, active: false, ct);

    private async Task<Result<WorkflowResponse>> SetActive(Guid id, bool active, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId) return WorkflowErrors.Unauthorized;

        var workflow = await db.Workflows
            .Include(w => w.Steps.OrderBy(s => s.OrderIndex))
            .SingleOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct)
            .ConfigureAwait(false);

        if (workflow is null) return WorkflowErrors.NotFound;

        if (active) workflow.Activate(clock.UtcNow);
        else workflow.Deactivate(clock.UtcNow);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        if (workflow.TriggerType == TriggerType.Schedule)
        {
            if (workflow.IsActive) RegisterSchedule(workflow);
            else scheduleRegistrar.RemoveSchedule(workflow.Id);
        }

        return Map(workflow);
    }

    private void RegisterSchedule(Workflow workflow)
    {
        var config = ParseJson(workflow.TriggerConfig);
        if (config.ValueKind != JsonValueKind.Object) return;
        if (!config.TryGetProperty("cron", out var cron) || cron.ValueKind != JsonValueKind.String) return;
        var expression = cron.GetString();
        if (string.IsNullOrWhiteSpace(expression)) return;
        scheduleRegistrar.RegisterSchedule(workflow.Id, expression);
    }

    private Result<List<WorkflowStep>> BuildSteps(IReadOnlyList<StepInput> input)
    {
        var built = new List<WorkflowStep>(input.Count);
        var index = 0;
        foreach (var s in input.OrderBy(s => s.OrderIndex))
        {
            if (!ActionTypes.All.Contains(s.ActionType))
                return WorkflowErrors.InvalidActionType(s.ActionType);

            built.Add(WorkflowStep.Create(
                index++,
                TypeMapping.ParseAction(s.ActionType),
                SerializeJson(s.Config),
                clock.UtcNow));
        }
        return built;
    }

    private static WorkflowResponse Map(Workflow w)
    {
        return new WorkflowResponse(
            w.Id,
            w.Name,
            w.Description,
            TypeMapping.FormatTrigger(w.TriggerType),
            ParseJson(w.TriggerConfig),
            string.IsNullOrWhiteSpace(w.Layout) ? null : ParseJson(w.Layout),
            w.IsActive,
            w.CreatedAt,
            w.UpdatedAt,
            w.Steps
                .OrderBy(s => s.OrderIndex)
                .Select(s => new StepResponse(s.Id, s.OrderIndex, TypeMapping.FormatAction(s.ActionType), ParseJson(s.Config)))
                .ToList());
    }

    public async Task<Result<WorkflowResponse>> UpdateLayoutAsync(Guid id, UpdateLayoutRequest req, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId) return WorkflowErrors.Unauthorized;

        var workflow = await db.Workflows
            .Include(w => w.Steps)
            .SingleOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct)
            .ConfigureAwait(false);

        if (workflow is null) return WorkflowErrors.NotFound;

        var json = req.Layout.ValueKind == JsonValueKind.Object ? req.Layout.GetRawText() : null;
        workflow.SetLayout(json, clock.UtcNow);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return Map(workflow);
    }

    private static string SerializeJson(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Undefined) return "{}";
        return element.GetRawText();
    }

    private static JsonElement ParseJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return JsonDocument.Parse("{}").RootElement;
        return JsonDocument.Parse(raw).RootElement;
    }
}
