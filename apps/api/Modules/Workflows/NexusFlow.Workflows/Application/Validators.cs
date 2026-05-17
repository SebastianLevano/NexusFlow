using System.Text.Json;
using FluentValidation;

namespace NexusFlow.Workflows.Application;

internal static class TriggerTypes
{
    public const string Webhook = "webhook";
    public const string Schedule = "schedule";
    public static readonly string[] All = [Webhook, Schedule];
}

internal static class ActionTypes
{
    public const string HttpRequest = "http_request";
    public const string SaveToDatabase = "save_to_database";
    public const string SendNotification = "send_notification";
    public const string SlackPostMessage = "slack_post_message";
    public const string DiscordPostMessage = "discord_post_message";
    public static readonly string[] All =
    [
        HttpRequest, SaveToDatabase, SendNotification, SlackPostMessage, DiscordPostMessage,
    ];
}

internal sealed class StepInputValidator : AbstractValidator<StepInput>
{
    public StepInputValidator()
    {
        RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ActionType)
            .NotEmpty()
            .Must(t => ActionTypes.All.Contains(t))
            .WithMessage(x => $"Unknown action type '{x.ActionType}'. Allowed: {string.Join(", ", ActionTypes.All)}.");
        RuleFor(x => x.Config).Must(BeJsonObject).WithMessage("Step config must be a JSON object.");
    }

    private static bool BeJsonObject(JsonElement el) =>
        el.ValueKind == JsonValueKind.Undefined || el.ValueKind == JsonValueKind.Object;
}

internal sealed class CreateWorkflowRequestValidator : AbstractValidator<CreateWorkflowRequest>
{
    public CreateWorkflowRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.TriggerType)
            .NotEmpty()
            .Must(t => TriggerTypes.All.Contains(t))
            .WithMessage(x => $"Unknown trigger type '{x.TriggerType}'. Allowed: {string.Join(", ", TriggerTypes.All)}.");
        RuleFor(x => x.TriggerConfig).Must(BeJsonObject).WithMessage("Trigger config must be a JSON object.");
        RuleFor(x => x.Steps).NotNull();
        RuleForEach(x => x.Steps).SetValidator(new StepInputValidator());
    }

    private static bool BeJsonObject(JsonElement el) =>
        el.ValueKind == JsonValueKind.Undefined || el.ValueKind == JsonValueKind.Object;
}

internal sealed class UpdateWorkflowRequestValidator : AbstractValidator<UpdateWorkflowRequest>
{
    public UpdateWorkflowRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.TriggerType)
            .NotEmpty()
            .Must(t => TriggerTypes.All.Contains(t));
        RuleFor(x => x.TriggerConfig).Must(BeJsonObject).WithMessage("Trigger config must be a JSON object.");
        RuleFor(x => x.Steps).NotNull();
        RuleForEach(x => x.Steps).SetValidator(new StepInputValidator());
    }

    private static bool BeJsonObject(JsonElement el) =>
        el.ValueKind == JsonValueKind.Undefined || el.ValueKind == JsonValueKind.Object;
}
