using NexusFlow.Shared.Domain;

namespace NexusFlow.Workflows.Domain;

public sealed class WorkflowStep : BaseEntity
{
    private WorkflowStep() { }

    public Guid WorkflowId { get; private set; }
    public int OrderIndex { get; private set; }
    public ActionType ActionType { get; private set; }
    public string Config { get; private set; } = "{}";

    public static WorkflowStep Create(int orderIndex, ActionType actionType, string config, DateTimeOffset now)
    {
        return new WorkflowStep
        {
            OrderIndex = orderIndex,
            ActionType = actionType,
            Config = config,
            CreatedAt = now,
        };
    }

    internal void SetOrderIndex(int index) => OrderIndex = index;
}
