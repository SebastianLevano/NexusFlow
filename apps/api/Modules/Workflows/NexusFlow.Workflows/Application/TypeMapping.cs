using NexusFlow.Workflows.Domain;

namespace NexusFlow.Workflows.Application;

internal static class TypeMapping
{
    public static TriggerType ParseTrigger(string value) => value switch
    {
        TriggerTypes.Webhook => TriggerType.Webhook,
        TriggerTypes.Schedule => TriggerType.Schedule,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown trigger type."),
    };

    public static string FormatTrigger(TriggerType value) => value switch
    {
        TriggerType.Webhook => TriggerTypes.Webhook,
        TriggerType.Schedule => TriggerTypes.Schedule,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown trigger type."),
    };

    public static ActionType ParseAction(string value) => value switch
    {
        ActionTypes.HttpRequest => ActionType.HttpRequest,
        ActionTypes.SaveToDatabase => ActionType.SaveToDatabase,
        ActionTypes.SendNotification => ActionType.SendNotification,
        ActionTypes.SlackPostMessage => ActionType.SlackPostMessage,
        ActionTypes.DiscordPostMessage => ActionType.DiscordPostMessage,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown action type."),
    };

    public static string FormatAction(ActionType value) => value switch
    {
        ActionType.HttpRequest => ActionTypes.HttpRequest,
        ActionType.SaveToDatabase => ActionTypes.SaveToDatabase,
        ActionType.SendNotification => ActionTypes.SendNotification,
        ActionType.SlackPostMessage => ActionTypes.SlackPostMessage,
        ActionType.DiscordPostMessage => ActionTypes.DiscordPostMessage,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown action type."),
    };
}
