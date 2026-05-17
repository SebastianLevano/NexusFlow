using NexusFlow.Shared.Domain;

namespace NexusFlow.Executions.Domain;

public sealed class WorkflowOutput : BaseEntity
{
    private WorkflowOutput() { }

    public Guid WorkflowId { get; private set; }
    public Guid ExecutionId { get; private set; }
    public string Payload { get; private set; } = "{}";

    public static WorkflowOutput Create(Guid workflowId, Guid executionId, string payload, DateTimeOffset now)
    {
        return new WorkflowOutput
        {
            WorkflowId = workflowId,
            ExecutionId = executionId,
            Payload = payload,
            CreatedAt = now,
        };
    }
}
