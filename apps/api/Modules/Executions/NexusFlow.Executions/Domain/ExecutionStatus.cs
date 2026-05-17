namespace NexusFlow.Executions.Domain;

public enum ExecutionStatus
{
    Pending = 1,
    Running = 2,
    Succeeded = 3,
    Failed = 4,
}

public enum StepStatus
{
    Pending = 1,
    Running = 2,
    Succeeded = 3,
    Failed = 4,
    Skipped = 5,
}
