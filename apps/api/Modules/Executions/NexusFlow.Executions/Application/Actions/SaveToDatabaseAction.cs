using System.Text.Json;
using NexusFlow.Executions.Domain;
using NexusFlow.Executions.Infrastructure;
using NexusFlow.Executions.Application.Templating;
using NexusFlow.Shared.Time;

namespace NexusFlow.Executions.Application.Actions;

internal sealed class SaveToDatabaseAction(ExecutionsDbContext db, IClock clock) : IActionHandler
{
    public string ActionType => "save_to_database";

    public async Task<ActionResult> ExecuteAsync(ActionInvocation invocation, CancellationToken ct)
    {
        var resolved = Templater.ResolveJson(invocation.Config, invocation.Context);

        // If config has a "payload" field, save just that. Otherwise save the trigger payload.
        JsonElement payload;
        if (resolved.TryGetProperty("payload", out var explicitPayload))
        {
            payload = explicitPayload;
        }
        else if (invocation.Context.TryResolve("trigger", out var trigger))
        {
            payload = trigger;
        }
        else
        {
            payload = Templater.EmptyObject();
        }

        var output = WorkflowOutput.Create(
            invocation.WorkflowId,
            invocation.ExecutionId,
            payload.GetRawText(),
            clock.UtcNow);

        db.WorkflowOutputs.Add(output);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        using var doc = JsonDocument.Parse($$"""{"savedId":"{{output.Id}}","bytes":{{payload.GetRawText().Length}}}""");
        return new ActionResult(doc.RootElement.Clone());
    }
}
