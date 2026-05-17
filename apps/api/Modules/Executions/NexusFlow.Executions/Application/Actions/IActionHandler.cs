using System.Text.Json;
using NexusFlow.Executions.Application.Templating;

namespace NexusFlow.Executions.Application.Actions;

public sealed record ActionInvocation(
    Guid ExecutionId,
    Guid WorkflowId,
    Guid UserId,
    JsonElement Config,
    WorkflowExecutionContext Context);

public sealed record ActionResult(JsonElement Output);

public interface IActionHandler
{
    string ActionType { get; }

    Task<ActionResult> ExecuteAsync(ActionInvocation invocation, CancellationToken ct);
}
