using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NexusFlow.Executions.Infrastructure;

namespace NexusFlow.Executions.Realtime;

[Authorize]
public sealed class ExecutionHub(ExecutionsDbContext db) : Hub<IExecutionClient>
{
    internal const string GroupPrefix = "exec:";

    public async Task<JoinResult> Join(Guid executionId)
    {
        if (TryGetUserId(out var userId))
        {
            var exists = await db.Executions
                .AsNoTracking()
                .AnyAsync(e => e.Id == executionId && e.UserId == userId)
                .ConfigureAwait(false);

            if (!exists) return new JoinResult(false, "not_found");
        }
        else
        {
            return new JoinResult(false, "unauthenticated");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(executionId)).ConfigureAwait(false);
        return new JoinResult(true, null);
    }

    public Task Leave(Guid executionId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(executionId));
    }

    internal static string GroupName(Guid executionId) => $"{GroupPrefix}{executionId:N}";

    private bool TryGetUserId(out Guid userId)
    {
        userId = default;
        var sub = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? Context.User?.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out userId);
    }
}

public sealed record JoinResult(bool Ok, string? Reason);
