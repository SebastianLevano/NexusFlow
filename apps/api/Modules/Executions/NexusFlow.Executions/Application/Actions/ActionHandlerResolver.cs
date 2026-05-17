namespace NexusFlow.Executions.Application.Actions;

public sealed class ActionHandlerResolver
{
    private readonly Dictionary<string, IActionHandler> _byType;

    public ActionHandlerResolver(IEnumerable<IActionHandler> handlers)
    {
        _byType = handlers.ToDictionary(h => h.ActionType, StringComparer.OrdinalIgnoreCase);
    }

    public IActionHandler Resolve(string actionType)
    {
        if (!_byType.TryGetValue(actionType, out var handler))
            throw new InvalidOperationException($"No handler registered for action '{actionType}'.");
        return handler;
    }
}
