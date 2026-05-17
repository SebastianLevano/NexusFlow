namespace NexusFlow.Integrations.Abstractions;

public interface ISlackClient
{
    Task<ProviderPostResult> PostMessageAsync(string webhookUrl, string text, CancellationToken ct);
}

public interface IDiscordClient
{
    Task<ProviderPostResult> PostMessageAsync(string webhookUrl, string text, CancellationToken ct);
}

public sealed record ProviderPostResult(bool Ok, int StatusCode, string? Body, string? Error);
