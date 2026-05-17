using System.Text.Json;

namespace NexusFlow.Executions.Application;

public sealed record ExecutionSummary(
    Guid Id,
    Guid WorkflowId,
    string Status,
    string TriggeredBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    int? DurationMs,
    string? ErrorMessage,
    int StepCount);

public sealed record StepExecutionResponse(
    Guid Id,
    Guid StepId,
    int OrderIndex,
    string ActionType,
    string Status,
    JsonElement Input,
    JsonElement Output,
    string? Error,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    int? DurationMs);

public sealed record ExecutionResponse(
    Guid Id,
    Guid WorkflowId,
    string Status,
    string TriggeredBy,
    JsonElement TriggerPayload,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    int? DurationMs,
    string? ErrorMessage,
    IReadOnlyList<StepExecutionResponse> Steps);

public sealed record ExecutionStats(
    int WorkflowsActive,
    int WorkflowsTotal,
    int Runs24h,
    int Runs7d,
    int SucceededLast24h,
    int FailedLast24h,
    double SuccessRateLast24h,
    int? AvgDurationMsLast24h,
    int? P50DurationMsLast24h,
    int? P95DurationMsLast24h);

public sealed record ExecutionTimeseriesPoint(
    DateTimeOffset Bucket,
    int Succeeded,
    int Failed,
    int Running,
    int Pending);

public sealed record ExecutionTimeseriesResponse(
    string Range,
    string Interval,
    IReadOnlyList<ExecutionTimeseriesPoint> Points);
