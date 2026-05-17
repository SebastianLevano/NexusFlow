using NexusFlow.Shared.Domain;

namespace NexusFlow.Executions.Domain;

public sealed class StepExecution : BaseEntity
{
    private StepExecution() { }

    public Guid ExecutionId { get; private set; }
    public Guid StepId { get; private set; }
    public int OrderIndex { get; private set; }
    public string ActionType { get; private set; } = default!;
    public StepStatus Status { get; private set; }
    public string Input { get; private set; } = "{}";
    public string Output { get; private set; } = "{}";
    public string? Error { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public int? DurationMs { get; private set; }

    internal static StepExecution Create(Guid executionId, Guid stepId, int orderIndex, string actionType, DateTimeOffset now)
    {
        return new StepExecution
        {
            ExecutionId = executionId,
            StepId = stepId,
            OrderIndex = orderIndex,
            ActionType = actionType,
            Status = StepStatus.Pending,
            CreatedAt = now,
        };
    }

    public void Start(string inputJson, DateTimeOffset now)
    {
        Status = StepStatus.Running;
        Input = inputJson;
        StartedAt = now;
        UpdatedAt = now;
    }

    public void Succeed(string outputJson, DateTimeOffset now)
    {
        Status = StepStatus.Succeeded;
        Output = outputJson;
        FinishedAt = now;
        DurationMs = StartedAt is null ? 0 : (int)(now - StartedAt.Value).TotalMilliseconds;
        UpdatedAt = now;
    }

    public void Fail(string error, string? outputJson, DateTimeOffset now)
    {
        Status = StepStatus.Failed;
        Error = error;
        if (outputJson is not null) Output = outputJson;
        FinishedAt = now;
        DurationMs = StartedAt is null ? 0 : (int)(now - StartedAt.Value).TotalMilliseconds;
        UpdatedAt = now;
    }

    public void Skip(DateTimeOffset now)
    {
        Status = StepStatus.Skipped;
        FinishedAt = now;
        UpdatedAt = now;
    }
}
