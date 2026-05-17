using NexusFlow.Executions.Abstractions;
using NexusFlow.Shared.Domain;

namespace NexusFlow.Executions.Domain;

public sealed class Execution : BaseEntity
{
    private Execution() { }

    public Guid WorkflowId { get; private set; }
    public Guid UserId { get; private set; }
    public ExecutionStatus Status { get; private set; }
    public TriggerSource TriggeredBy { get; private set; }
    public string TriggerPayload { get; private set; } = "{}";
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public int? DurationMs { get; private set; }
    public string? ErrorMessage { get; private set; }

    private readonly List<StepExecution> _steps = [];
    public IReadOnlyList<StepExecution> Steps => _steps.AsReadOnly();

    public static Execution Create(
        Guid workflowId,
        Guid userId,
        TriggerSource triggeredBy,
        string triggerPayload,
        DateTimeOffset now)
    {
        return new Execution
        {
            WorkflowId = workflowId,
            UserId = userId,
            Status = ExecutionStatus.Pending,
            TriggeredBy = triggeredBy,
            TriggerPayload = triggerPayload,
            CreatedAt = now,
        };
    }

    public void Start(DateTimeOffset now)
    {
        Status = ExecutionStatus.Running;
        StartedAt = now;
        UpdatedAt = now;
    }

    public void Succeed(DateTimeOffset now)
    {
        Status = ExecutionStatus.Succeeded;
        FinishedAt = now;
        DurationMs = StartedAt is null ? 0 : (int)(now - StartedAt.Value).TotalMilliseconds;
        UpdatedAt = now;
    }

    public void Fail(DateTimeOffset now, string error)
    {
        Status = ExecutionStatus.Failed;
        FinishedAt = now;
        DurationMs = StartedAt is null ? 0 : (int)(now - StartedAt.Value).TotalMilliseconds;
        ErrorMessage = error;
        UpdatedAt = now;
    }

    public StepExecution AddStep(Guid stepId, int orderIndex, string actionType, DateTimeOffset now)
    {
        var s = StepExecution.Create(Id, stepId, orderIndex, actionType, now);
        _steps.Add(s);
        return s;
    }
}
