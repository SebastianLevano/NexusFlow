using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NexusFlow.Executions.Application.Templating;

public static partial class Templater
{
    [GeneratedRegex(@"\{\{\s*([a-zA-Z0-9_.\[\]]+)\s*\}\}", RegexOptions.Compiled)]
    private static partial Regex TemplateRegex();

    public static string ResolveString(string input, WorkflowExecutionContext context)
    {
        if (string.IsNullOrEmpty(input)) return input;

        return TemplateRegex().Replace(input, match =>
        {
            var path = match.Groups[1].Value;
            return context.TryResolve(path, out var value) ? Stringify(value) : string.Empty;
        });
    }

    public static JsonElement ResolveJson(JsonElement source, WorkflowExecutionContext context)
    {
        using var doc = JsonDocument.Parse(WriteResolved(source, context));
        return doc.RootElement.Clone();
    }

    private static string WriteResolved(JsonElement source, WorkflowExecutionContext context)
    {
        using var ms = new MemoryStream();
        using (var writer = new Utf8JsonWriter(ms))
        {
            WriteElement(writer, source, context);
        }
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static void WriteElement(Utf8JsonWriter writer, JsonElement element, WorkflowExecutionContext context)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var prop in element.EnumerateObject())
                {
                    writer.WritePropertyName(prop.Name);
                    WriteElement(writer, prop.Value, context);
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteElement(writer, item, context);
                }
                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                WriteResolvedString(writer, element.GetString() ?? string.Empty, context);
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static void WriteResolvedString(Utf8JsonWriter writer, string raw, WorkflowExecutionContext context)
    {
        var match = TemplateRegex().Match(raw);
        if (!match.Success)
        {
            writer.WriteStringValue(raw);
            return;
        }

        // If the entire string is a single template expression, keep the resolved value's type.
        if (match.Index == 0 && match.Length == raw.Length)
        {
            if (context.TryResolve(match.Groups[1].Value, out var value))
            {
                value.WriteTo(writer);
                return;
            }
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(ResolveString(raw, context));
    }

    private static string Stringify(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.Number => value.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
        _ => value.GetRawText(),
    };

    public static JsonElement EmptyObject()
    {
        using var doc = JsonDocument.Parse("{}");
        return doc.RootElement.Clone();
    }

    public static JsonElement ParseOrEmpty(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return EmptyObject();
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return EmptyObject();
        }
    }

    public static string FormatInvariant(int value) => value.ToString(CultureInfo.InvariantCulture);
}
