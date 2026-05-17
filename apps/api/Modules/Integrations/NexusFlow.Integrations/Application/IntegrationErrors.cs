using NexusFlow.Shared.Results;

namespace NexusFlow.Integrations.Application;

public static class IntegrationErrors
{
    public static readonly Error Unauthorized = new("integrations.unauthorized", "Authentication required.", ErrorType.Unauthorized);
    public static readonly Error NotFound = new("integrations.not_found", "Integration not found.", ErrorType.NotFound);
    public static readonly Error InvalidProvider = new("integrations.invalid_provider", "Provider must be 'slack' or 'discord'.", ErrorType.Validation);
    public static readonly Error InvalidWebhookUrl = new("integrations.invalid_webhook_url", "Webhook URL is invalid.", ErrorType.Validation);
    public static readonly Error LabelRequired = new("integrations.label_required", "Label is required.", ErrorType.Validation);
}
