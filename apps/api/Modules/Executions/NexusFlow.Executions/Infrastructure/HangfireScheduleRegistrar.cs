using Hangfire;
using NexusFlow.Executions.Abstractions;

namespace NexusFlow.Executions.Infrastructure;

internal sealed class HangfireScheduleRegistrar(IRecurringJobManager recurring) : IScheduleRegistrar
{
    public void RegisterSchedule(Guid workflowId, string cronExpression)
    {
        var jobId = JobId(workflowId);
        recurring.AddOrUpdate<IExecutionScheduler>(
            jobId,
            s => s.EnqueueAsync(workflowId, TriggerSource.Schedule, null, CancellationToken.None),
            cronExpression);
    }

    public void RemoveSchedule(Guid workflowId)
    {
        recurring.RemoveIfExists(JobId(workflowId));
    }

    private static string JobId(Guid workflowId) => $"wf-{workflowId}";
}
