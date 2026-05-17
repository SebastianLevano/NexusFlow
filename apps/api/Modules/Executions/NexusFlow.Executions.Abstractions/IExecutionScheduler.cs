namespace NexusFlow.Executions.Abstractions;

public enum TriggerSource
{
    Webhook = 1,
    Schedule = 2,
    Manual = 3,
}

public interface IExecutionScheduler
{
    Task<Guid> EnqueueAsync(Guid workflowId, TriggerSource source, string? triggerPayloadJson, CancellationToken ct = default);
}

public interface IScheduleRegistrar
{
    void RegisterSchedule(Guid workflowId, string cronExpression);
    void RemoveSchedule(Guid workflowId);
}
