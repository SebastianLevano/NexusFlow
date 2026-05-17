using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NexusFlow.Executions.Application.Templating;

namespace NexusFlow.Executions.Application.Actions;

internal sealed class HttpRequestAction(IHttpClientFactory factory) : IActionHandler
{
    public string ActionType => "http_request";

    public async Task<ActionResult> ExecuteAsync(ActionInvocation invocation, CancellationToken ct)
    {
        var resolved = Templater.ResolveJson(invocation.Config, invocation.Context);

        var method = (resolved.TryGetProperty("method", out var m) ? m.GetString() : "GET")?.ToUpperInvariant() ?? "GET";
        var url = resolved.TryGetProperty("url", out var u) ? u.GetString() : null;
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("HTTP action requires a 'url'.");

        using var request = new HttpRequestMessage(new HttpMethod(method), url);

        if (resolved.TryGetProperty("headers", out var headers) && headers.ValueKind == JsonValueKind.Object)
        {
            foreach (var header in headers.EnumerateObject())
            {
                request.Headers.TryAddWithoutValidation(header.Name, header.Value.GetString());
            }
        }

        if (resolved.TryGetProperty("body", out var body) && body.ValueKind != JsonValueKind.Undefined && body.ValueKind != JsonValueKind.Null)
        {
            var json = body.GetRawText();
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var client = factory.CreateClient("nexusflow-action-http");
        client.Timeout = TimeSpan.FromSeconds(30);

        using var response = await client.SendAsync(request, ct).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        var output = BuildOutput(response, content);
        return new ActionResult(output);
    }

    private static JsonElement BuildOutput(HttpResponseMessage response, string content)
    {
        using var ms = new MemoryStream();
        using (var writer = new Utf8JsonWriter(ms))
        {
            writer.WriteStartObject();
            writer.WriteNumber("status", (int)response.StatusCode);
            writer.WriteBoolean("ok", response.IsSuccessStatusCode);

            writer.WritePropertyName("headers");
            writer.WriteStartObject();
            WriteHeaders(writer, response.Headers);
            WriteHeaders(writer, response.Content.Headers);
            writer.WriteEndObject();

            writer.WritePropertyName("body");
            WriteBody(writer, content, response.Content.Headers.ContentType);

            writer.WriteEndObject();
        }

        using var doc = JsonDocument.Parse(ms.ToArray());
        return doc.RootElement.Clone();
    }

    private static void WriteHeaders(Utf8JsonWriter writer, HttpHeaders headers)
    {
        foreach (var (key, values) in headers)
        {
            writer.WriteString(key, string.Join(", ", values));
        }
    }

    private static void WriteBody(Utf8JsonWriter writer, string content, MediaTypeHeaderValue? contentType)
    {
        if (contentType?.MediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true && !string.IsNullOrWhiteSpace(content))
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                doc.RootElement.WriteTo(writer);
                return;
            }
            catch (JsonException)
            {
                // fall through to string
            }
        }

        writer.WriteStringValue(content);
    }
}
