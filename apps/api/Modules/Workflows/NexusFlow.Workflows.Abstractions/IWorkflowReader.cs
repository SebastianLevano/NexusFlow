namespace NexusFlow.Workflows.Abstractions;

public interface IWorkflowReader
{
    Task<WorkflowSnapshot?> GetActiveWorkflowAsync(Guid workflowId, CancellationToken ct = default);

    Task<WorkflowDefinition?> GetDefinitionAsync(Guid workflowId, CancellationToken ct = default);

    Task<IReadOnlyList<WorkflowDefinition>> GetActiveSchedulesAsync(CancellationToken ct = default);

    Task<WorkflowCounts> GetCountsForUserAsync(Guid userId, CancellationToken ct = default);
}

public sealed record WorkflowCounts(int Total, int Active);

public sealed record WorkflowSnapshot(
    Guid Id,
    Guid UserId,
    string Name,
    string TriggerType,
    bool IsActive);

public sealed record WorkflowDefinition(
    Guid Id,
    Guid UserId,
    string Name,
    string TriggerType,
    string TriggerConfigJson,
    bool IsActive,
    IReadOnlyList<WorkflowStepDefinition> Steps);

public sealed record WorkflowStepDefinition(
    Guid Id,
    int OrderIndex,
    string ActionType,
    string ConfigJson);
