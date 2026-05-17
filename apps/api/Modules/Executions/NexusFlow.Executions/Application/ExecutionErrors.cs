using NexusFlow.Shared.Results;

namespace NexusFlow.Executions.Application;

public static class ExecutionErrors
{
    public static readonly Error NotFound =
        Error.NotFound("executions.not_found", "Execution not found.");

    public static readonly Error Unauthorized =
        Error.Unauthorized("executions.unauthorized", "Authentication required.");

    public static readonly Error WorkflowInactive =
        Error.Conflict("executions.workflow_inactive", "Workflow is not active.");

    public static readonly Error WorkflowNotFound =
        Error.NotFound("executions.workflow_not_found", "Workflow not found.");

    public static readonly Error InvalidWebhookSecret =
        Error.Unauthorized("executions.invalid_webhook_secret", "Webhook secret does not match.");
}
