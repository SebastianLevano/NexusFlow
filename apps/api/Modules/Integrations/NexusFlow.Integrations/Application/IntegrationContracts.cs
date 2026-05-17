namespace NexusFlow.Integrations.Application;

public sealed record IntegrationSummary(
    Guid Id,
    string Provider,
    string Label,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt);

public sealed record CreateIntegrationRequest(
    string Provider,
    string Label,
    string WebhookUrl);

public sealed record TestMessageRequest(string? Text);

public sealed record TestMessageResponse(bool Ok, int StatusCode, string? Error);
