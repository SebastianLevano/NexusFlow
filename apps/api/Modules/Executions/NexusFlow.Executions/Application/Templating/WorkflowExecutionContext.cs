using System.Text.Json;

namespace NexusFlow.Executions.Application.Templating;

public sealed class WorkflowExecutionContext
{
    private readonly Dictionary<string, JsonElement> _scope = new(StringComparer.OrdinalIgnoreCase);

    public WorkflowExecutionContext(JsonElement trigger)
    {
        _scope["trigger"] = trigger;
    }

    public void SetStepOutput(int stepNumberOneBased, JsonElement output)
    {
        _scope[$"step{stepNumberOneBased}"] = output;
    }

    public bool TryResolve(string path, out JsonElement value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(path)) return false;

        var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;

        if (!_scope.TryGetValue(parts[0], out var current)) return false;

        for (var i = 1; i < parts.Length; i++)
        {
            var segment = parts[i];

            if (current.ValueKind == JsonValueKind.Object)
            {
                if (!current.TryGetProperty(segment, out var next)) return false;
                current = next;
                continue;
            }

            if (current.ValueKind == JsonValueKind.Array && int.TryParse(segment, out var idx) && idx >= 0 && idx < current.GetArrayLength())
            {
                current = current[idx];
                continue;
            }

            return false;
        }

        value = current;
        return true;
    }
}
