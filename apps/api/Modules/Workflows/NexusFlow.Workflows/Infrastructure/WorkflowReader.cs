using Microsoft.EntityFrameworkCore;
using NexusFlow.Workflows.Abstractions;
using NexusFlow.Workflows.Application;
using NexusFlow.Workflows.Domain;

namespace NexusFlow.Workflows.Infrastructure;

internal sealed class WorkflowReader(WorkflowsDbContext db) : IWorkflowReader
{
    public async Task<WorkflowSnapshot?> GetActiveWorkflowAsync(Guid workflowId, CancellationToken ct = default)
    {
        var w = await db.Workflows
            .AsNoTracking()
            .Where(x => x.Id == workflowId && x.IsActive)
            .Select(x => new { x.Id, x.UserId, x.Name, x.TriggerType, x.IsActive })
            .SingleOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (w is null) return null;
        return new WorkflowSnapshot(w.Id, w.UserId, w.Name, TypeMapping.FormatTrigger(w.TriggerType), w.IsActive);
    }

    public async Task<WorkflowDefinition?> GetDefinitionAsync(Guid workflowId, CancellationToken ct = default)
    {
        var w = await db.Workflows
            .AsNoTracking()
            .Include(x => x.Steps.OrderBy(s => s.OrderIndex))
            .SingleOrDefaultAsync(x => x.Id == workflowId, ct)
            .ConfigureAwait(false);

        return w is null ? null : Map(w);
    }

    public async Task<IReadOnlyList<WorkflowDefinition>> GetActiveSchedulesAsync(CancellationToken ct = default)
    {
        var list = await db.Workflows
            .AsNoTracking()
            .Where(x => x.IsActive && x.TriggerType == TriggerType.Schedule)
            .Include(x => x.Steps.OrderBy(s => s.OrderIndex))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return list.Select(Map).ToList();
    }

    public async Task<WorkflowCounts> GetCountsForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var counts = await db.Workflows
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .GroupBy(w => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Sum(w => w.IsActive ? 1 : 0),
            })
            .SingleOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return counts is null ? new WorkflowCounts(0, 0) : new WorkflowCounts(counts.Total, counts.Active);
    }

    private static WorkflowDefinition Map(Workflow w) => new(
        w.Id,
        w.UserId,
        w.Name,
        TypeMapping.FormatTrigger(w.TriggerType),
        w.TriggerConfig,
        w.IsActive,
        w.Steps
            .OrderBy(s => s.OrderIndex)
            .Select(s => new WorkflowStepDefinition(s.Id, s.OrderIndex, TypeMapping.FormatAction(s.ActionType), s.Config))
            .ToList());
}
