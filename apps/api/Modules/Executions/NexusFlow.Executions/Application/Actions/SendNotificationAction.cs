using System.Text.Json;
using Microsoft.Extensions.Logging;
using NexusFlow.Executions.Application.Templating;

namespace NexusFlow.Executions.Application.Actions;

internal sealed class SendNotificationAction(ILogger<SendNotificationAction> logger) : IActionHandler
{
    public string ActionType => "send_notification";

    public Task<ActionResult> ExecuteAsync(ActionInvocation invocation, CancellationToken ct)
    {
        var resolved = Templater.ResolveJson(invocation.Config, invocation.Context);
        var message = resolved.TryGetProperty("message", out var m) ? m.GetString() ?? string.Empty : string.Empty;

        logger.LogInformation(
            "Notification (workflow {WorkflowId}, execution {ExecutionId}): {Message}",
            invocation.WorkflowId, invocation.ExecutionId, message);

        using var doc = JsonDocument.Parse($$"""{"delivered":true,"channel":"log","message":{{JsonSerializer.Serialize(message)}}}""");
        return Task.FromResult(new ActionResult(doc.RootElement.Clone()));
    }
}
