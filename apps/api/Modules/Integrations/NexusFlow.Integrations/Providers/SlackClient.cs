using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NexusFlow.Integrations.Abstractions;

namespace NexusFlow.Integrations.Providers;

internal sealed class SlackClient(IHttpClientFactory httpClientFactory, ILogger<SlackClient> logger) : ISlackClient
{
    public const string HttpClientName = "integrations.slack";

    public async Task<ProviderPostResult> PostMessageAsync(string webhookUrl, string text, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
            return new ProviderPostResult(false, 0, null, "Empty webhook URL.");

        if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var uri))
            return new ProviderPostResult(false, 0, null, "Invalid webhook URL.");

        var http = httpClientFactory.CreateClient(HttpClientName);

        try
        {
            using var response = await http
                .PostAsJsonAsync(uri, new { text }, JsonSerializerOptions.Default, ct)
                .ConfigureAwait(false);

            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (response.IsSuccessStatusCode) return new ProviderPostResult(true, (int)response.StatusCode, body, null);
            return new ProviderPostResult(false, (int)response.StatusCode, body, $"Slack returned {(int)response.StatusCode}.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Slack webhook call failed.");
            return new ProviderPostResult(false, 0, null, ex.Message);
        }
    }
}
