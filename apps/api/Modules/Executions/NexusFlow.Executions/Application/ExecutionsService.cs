using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NexusFlow.Auth.Abstractions;
using NexusFlow.Executions.Abstractions;
using NexusFlow.Executions.Application.Templating;
using NexusFlow.Executions.Domain;
using NexusFlow.Executions.Infrastructure;
using NexusFlow.Shared.Results;
using NexusFlow.Shared.Time;
using NexusFlow.Workflows.Abstractions;

namespace NexusFlow.Executions.Application;

public sealed class ExecutionsService(
    ExecutionsDbContext db,
    IWorkflowReader workflows,
    IExecutionScheduler scheduler,
    ICurrentUser currentUser,
    IClock clock)
{
    public async Task<Result<IReadOnlyList<ExecutionSummary>>> ListAsync(Guid? workflowId, string? status, string? range, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId) return ExecutionErrors.Unauthorized;

        var query = db.Executions
            .AsNoTracking()
            .Where(e => e.UserId == userId);

        if (workflowId is { } wid) query = query.Where(e => e.WorkflowId == wid);

        if (TryParseStatus(status, out var s)) query = query.Where(e => e.Status == s);

        var since = range switch
        {
            "1h" => (DateTimeOffset?)clock.UtcNow.AddHours(-1),
            "24h" => clock.UtcNow.AddHours(-24),
            "7d" => clock.UtcNow.AddDays(-7),
            "30d" => clock.UtcNow.AddDays(-30),
            _ => null,
        };
        if (since is { } cutoff) query = query.Where(e => e.CreatedAt >= cutoff);

        var rows = await query
            .OrderByDescending(e => e.CreatedAt)
            .Take(200)
            .Select(e => new
            {
                e.Id,
                e.WorkflowId,
                e.Status,
                e.TriggeredBy,
                e.CreatedAt,
                e.StartedAt,
                e.FinishedAt,
                e.DurationMs,
                e.ErrorMessage,
                StepCount = e.Steps.Count,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        IReadOnlyList<ExecutionSummary> result = rows
            .Select(r => new ExecutionSummary(
                r.Id, r.WorkflowId,
                r.Status.ToString().ToLowerInvariant(),
                FormatTrigger(r.TriggeredBy),
                r.CreatedAt, r.StartedAt, r.FinishedAt, r.DurationMs, r.ErrorMessage, r.StepCount))
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<ExecutionResponse>> GetAsync(Guid id, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId) return ExecutionErrors.Unauthorized;

        var execution = await db.Executions
            .AsNoTracking()
            .Include(e => e.Steps.OrderBy(s => s.OrderIndex))
            .SingleOrDefaultAsync(e => e.Id == id && e.UserId == userId, ct)
            .ConfigureAwait(false);

        if (execution is null) return ExecutionErrors.NotFound;
        return Map(execution);
    }

    public async Task<Result<Guid>> TriggerManualAsync(Guid workflowId, JsonElement? payload, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId) return ExecutionErrors.Unauthorized;

        var definition = await workflows.GetDefinitionAsync(workflowId, ct).ConfigureAwait(false);
        if (definition is null || definition.UserId != userId) return ExecutionErrors.WorkflowNotFound;

        var raw = payload?.ValueKind == JsonValueKind.Object ? payload.Value.GetRawText() : null;
        var executionId = await scheduler.EnqueueAsync(workflowId, TriggerSource.Manual, raw, ct).ConfigureAwait(false);
        return executionId;
    }

    public async Task<Result<Guid>> TriggerWebhookAsync(Guid workflowId, string secret, JsonElement? payload, CancellationToken ct)
    {
        var definition = await workflows.GetDefinitionAsync(workflowId, ct).ConfigureAwait(false);
        if (definition is null) return ExecutionErrors.WorkflowNotFound;
        if (!definition.IsActive) return ExecutionErrors.WorkflowInactive;
        if (!string.Equals(definition.TriggerType, "webhook", StringComparison.Ordinal))
            return ExecutionErrors.WorkflowInactive;

        var triggerConfig = Templater.ParseOrEmpty(definition.TriggerConfigJson);
        var expected = triggerConfig.TryGetProperty("secret", out var s) ? s.GetString() : null;
        if (string.IsNullOrEmpty(expected) || !string.Equals(expected, secret, StringComparison.Ordinal))
            return ExecutionErrors.InvalidWebhookSecret;

        var raw = payload?.ValueKind == JsonValueKind.Object ? payload.Value.GetRawText() : null;
        var executionId = await scheduler.EnqueueAsync(workflowId, TriggerSource.Webhook, raw, ct).ConfigureAwait(false);
        _ = clock;
        return executionId;
    }

    public async Task<Guid> CreatePendingExecutionAsync(Guid workflowId, Guid userId, TriggerSource source, string? triggerPayloadJson, CancellationToken ct)
    {
        var execution = Execution.Create(
            workflowId,
            userId,
            source,
            triggerPayloadJson ?? "{}",
            clock.UtcNow);

        db.Executions.Add(execution);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return execution.Id;
    }

    private static ExecutionResponse Map(Execution e)
    {
        return new ExecutionResponse(
            e.Id,
            e.WorkflowId,
            e.Status.ToString().ToLowerInvariant(),
            FormatTrigger(e.TriggeredBy),
            Templater.ParseOrEmpty(e.TriggerPayload),
            e.CreatedAt,
            e.StartedAt,
            e.FinishedAt,
            e.DurationMs,
            e.ErrorMessage,
            e.Steps
                .OrderBy(s => s.OrderIndex)
                .Select(s => new StepExecutionResponse(
                    s.Id, s.StepId, s.OrderIndex, s.ActionType,
                    s.Status.ToString().ToLowerInvariant(),
                    Templater.ParseOrEmpty(s.Input),
                    Templater.ParseOrEmpty(s.Output),
                    s.Error, s.StartedAt, s.FinishedAt, s.DurationMs))
                .ToList());
    }

    private static string FormatTrigger(TriggerSource source) => source switch
    {
        TriggerSource.Webhook => "webhook",
        TriggerSource.Schedule => "schedule",
        TriggerSource.Manual => "manual",
        _ => "unknown",
    };

    private static bool TryParseStatus(string? status, out ExecutionStatus parsed)
    {
        parsed = default;
        if (string.IsNullOrWhiteSpace(status)) return false;
        return Enum.TryParse(status, ignoreCase: true, out parsed);
    }
}
