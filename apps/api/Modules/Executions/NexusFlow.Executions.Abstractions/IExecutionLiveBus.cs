namespace NexusFlow.Executions.Abstractions;

public interface IExecutionLiveBus
{
    Task PublishExecutionAsync(ExecutionLiveEvent evt, CancellationToken ct = default);

    Task PublishStepAsync(StepLiveEvent evt, CancellationToken ct = default);
}

public sealed record ExecutionLiveEvent(
    Guid ExecutionId,
    string Status,
    DateTimeOffset At,
    int? DurationMs = null,
    string? ErrorMessage = null);

public sealed record StepLiveEvent(
    Guid ExecutionId,
    Guid StepId,
    int OrderIndex,
    string ActionType,
    string Status,
    DateTimeOffset At,
    int? DurationMs = null,
    string? Error = null,
    string? InputJson = null,
    string? OutputJson = null);
