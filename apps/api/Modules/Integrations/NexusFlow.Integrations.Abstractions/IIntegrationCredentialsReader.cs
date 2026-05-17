namespace NexusFlow.Integrations.Abstractions;

public interface IIntegrationCredentialsReader
{
    Task<IntegrationCredentials?> GetAsync(Guid userId, Guid integrationId, CancellationToken ct = default);

    Task MarkUsedAsync(Guid integrationId, CancellationToken ct = default);
}

public sealed record IntegrationCredentials(
    Guid IntegrationId,
    Guid UserId,
    string Provider,
    string Label,
    string WebhookUrl);
