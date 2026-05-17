using NexusFlow.Shared.Results;

namespace NexusFlow.Workflows.Application;

public static class WorkflowErrors
{
    public static readonly Error NotFound =
        Error.NotFound("workflows.not_found", "Workflow not found.");

    public static readonly Error Unauthorized =
        Error.Unauthorized("workflows.unauthorized", "Authentication required.");

    public static Error InvalidTriggerType(string value) =>
        Error.Validation("workflows.invalid_trigger_type", $"Unknown trigger type '{value}'.");

    public static Error InvalidActionType(string value) =>
        Error.Validation("workflows.invalid_action_type", $"Unknown action type '{value}'.");
}
