using System.Text.Json;
using Microsoft.Extensions.Logging;
using NexusFlow.Executions.Application.Templating;
using NexusFlow.Integrations.Abstractions;

namespace NexusFlow.Executions.Application.Actions;

internal sealed class SlackPostMessageAction(
    ISlackClient slack,
    IIntegrationCredentialsReader credentials,
    ILogger<SlackPostMessageAction> logger) : IActionHandler
{
    public string ActionType => "slack_post_message";

    public Task<ActionResult> ExecuteAsync(ActionInvocation invocation, CancellationToken ct)
        => ProviderActions.SendAsync("slack", invocation, credentials, logger,
            (url, text, c) => slack.PostMessageAsync(url, text, c), ct);
}

internal sealed class DiscordPostMessageAction(
    IDiscordClient discord,
    IIntegrationCredentialsReader credentials,
    ILogger<DiscordPostMessageAction> logger) : IActionHandler
{
    public string ActionType => "discord_post_message";

    public Task<ActionResult> ExecuteAsync(ActionInvocation invocation, CancellationToken ct)
        => ProviderActions.SendAsync("discord", invocation, credentials, logger,
            (url, text, c) => discord.PostMessageAsync(url, text, c), ct);
}

internal static class ProviderActions
{
    public static async Task<ActionResult> SendAsync(
        string expectedProvider,
        ActionInvocation invocation,
        IIntegrationCredentialsReader credentials,
        ILogger logger,
        Func<string, string, CancellationToken, Task<ProviderPostResult>> send,
        CancellationToken ct)
    {
        if (!TryReadString(invocation.Config, "integrationId", out var integrationIdRaw)
            || !Guid.TryParse(integrationIdRaw, out var integrationId))
        {
            throw new InvalidOperationException("Step config requires 'integrationId' (UUID).");
        }

        if (!TryReadString(invocation.Config, "text", out var text) || string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Step config requires non-empty 'text'.");
        }

        var creds = await credentials.GetAsync(invocation.UserId, integrationId, ct).ConfigureAwait(false);
        if (creds is null)
        {
            throw new InvalidOperationException($"Integration {integrationId} not found or revoked.");
        }

        if (!string.Equals(creds.Provider, expectedProvider, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Integration {integrationId} is '{creds.Provider}', expected '{expectedProvider}'.");
        }

        var result = await send(creds.WebhookUrl, text!, ct).ConfigureAwait(false);
        if (!result.Ok)
        {
            logger.LogWarning("Provider {Provider} post failed ({Status}): {Error}",
                expectedProvider, result.StatusCode, result.Error);
            throw new InvalidOperationException(
                $"{expectedProvider} post failed: {result.Error ?? $"HTTP {result.StatusCode}"}");
        }

        await credentials.MarkUsedAsync(integrationId, ct).ConfigureAwait(false);

        var output = JsonSerializer.SerializeToElement(new
        {
            delivered = true,
            provider = expectedProvider,
            integrationId,
            statusCode = result.StatusCode,
        });
        return new ActionResult(output);
    }

    private static bool TryReadString(JsonElement config, string property, out string? value)
    {
        value = null;
        if (config.ValueKind != JsonValueKind.Object) return false;
        if (!config.TryGetProperty(property, out var prop)) return false;
        if (prop.ValueKind != JsonValueKind.String) return false;
        value = prop.GetString();
        return value is not null;
    }
}
