using NexusFlow.Executions.Abstractions;

namespace NexusFlow.Executions.Realtime;

public interface IExecutionClient
{
    Task ExecutionState(ExecutionLiveEvent evt);

    Task StepState(StepLiveEvent evt);
}
