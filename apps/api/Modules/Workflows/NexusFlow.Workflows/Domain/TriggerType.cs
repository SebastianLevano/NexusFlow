namespace NexusFlow.Workflows.Domain;

public enum TriggerType
{
    Webhook = 1,
    Schedule = 2,
}

public enum ActionType
{
    HttpRequest = 1,
    SaveToDatabase = 2,
    SendNotification = 3,
    SlackPostMessage = 4,
    DiscordPostMessage = 5,
}
