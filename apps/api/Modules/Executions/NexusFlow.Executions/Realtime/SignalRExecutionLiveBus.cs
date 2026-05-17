using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NexusFlow.Executions.Abstractions;

namespace NexusFlow.Executions.Realtime;

internal sealed class SignalRExecutionLiveBus(
    IHubContext<ExecutionHub, IExecutionClient> hub,
    ILogger<SignalRExecutionLiveBus> logger) : IExecutionLiveBus
{
    public async Task PublishExecutionAsync(ExecutionLiveEvent evt, CancellationToken ct = default)
    {
        try
        {
            await hub.Clients
                .Group(ExecutionHub.GroupName(evt.ExecutionId))
                .ExecutionState(evt)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish execution event for {ExecutionId}.", evt.ExecutionId);
        }
    }

    public async Task PublishStepAsync(StepLiveEvent evt, CancellationToken ct = default)
    {
        try
        {
            await hub.Clients
                .Group(ExecutionHub.GroupName(evt.ExecutionId))
                .StepState(evt)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish step event for {ExecutionId}/{StepId}.", evt.ExecutionId, evt.StepId);
        }
    }
}
