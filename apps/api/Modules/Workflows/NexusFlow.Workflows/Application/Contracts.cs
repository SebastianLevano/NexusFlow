using System.Text.Json;

namespace NexusFlow.Workflows.Application;

public sealed record StepInput(int OrderIndex, string ActionType, JsonElement Config);

public sealed record CreateWorkflowRequest(
    string Name,
    string? Description,
    string TriggerType,
    JsonElement TriggerConfig,
    IReadOnlyList<StepInput> Steps);

public sealed record UpdateWorkflowRequest(
    string Name,
    string? Description,
    string TriggerType,
    JsonElement TriggerConfig,
    IReadOnlyList<StepInput> Steps);

public sealed record WorkflowSummary(
    Guid Id,
    string Name,
    string? Description,
    string TriggerType,
    bool IsActive,
    int StepCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record StepResponse(
    Guid Id,
    int OrderIndex,
    string ActionType,
    JsonElement Config);

public sealed record WorkflowResponse(
    Guid Id,
    string Name,
    string? Description,
    string TriggerType,
    JsonElement TriggerConfig,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<StepResponse> Steps);
