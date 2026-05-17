using Microsoft.EntityFrameworkCore;
using NexusFlow.Auth.Abstractions;
using NexusFlow.Executions.Domain;
using NexusFlow.Executions.Infrastructure;
using NexusFlow.Shared.Results;
using NexusFlow.Shared.Time;
using NexusFlow.Workflows.Abstractions;

namespace NexusFlow.Executions.Application;

public sealed class ExecutionsStatsService(
    ExecutionsDbContext db,
    IWorkflowReader workflows,
    ICurrentUser currentUser,
    IClock clock)
{
    public async Task<Result<ExecutionStats>> GetStatsAsync(CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId) return ExecutionErrors.Unauthorized;

        var now = clock.UtcNow;
        var since24h = now.AddHours(-24);
        var since7d = now.AddDays(-7);

        var wfCounts = await workflows.GetCountsForUserAsync(userId, ct).ConfigureAwait(false);

        var scoped = db.Executions.AsNoTracking().Where(e => e.UserId == userId);

        var runs7d = await scoped.CountAsync(e => e.CreatedAt >= since7d, ct).ConfigureAwait(false);

        var last24h = await scoped
            .Where(e => e.CreatedAt >= since24h)
            .Select(e => new { e.Status, e.DurationMs })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var runs24h = last24h.Count;
        var succeeded = last24h.Count(x => x.Status == ExecutionStatus.Succeeded);
        var failed = last24h.Count(x => x.Status == ExecutionStatus.Failed);
        var terminal = succeeded + failed;
        var successRate = terminal == 0 ? 0d : (double)succeeded / terminal;

        var durations = last24h
            .Where(x => x.Status == ExecutionStatus.Succeeded && x.DurationMs.HasValue)
            .Select(x => x.DurationMs!.Value)
            .OrderBy(v => v)
            .ToArray();

        int? avg = durations.Length == 0 ? null : (int)durations.Average();
        int? p50 = durations.Length == 0 ? null : Percentile(durations, 0.50);
        int? p95 = durations.Length == 0 ? null : Percentile(durations, 0.95);

        return new ExecutionStats(
            wfCounts.Active,
            wfCounts.Total,
            runs24h,
            runs7d,
            succeeded,
            failed,
            Math.Round(successRate, 4),
            avg,
            p50,
            p95);
    }

    public async Task<Result<ExecutionTimeseriesResponse>> GetTimeseriesAsync(string range, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId) return ExecutionErrors.Unauthorized;

        var (since, bucket, label) = range switch
        {
            "7d" => (clock.UtcNow.AddDays(-7), TimeSpan.FromHours(6), "6h"),
            _ => (clock.UtcNow.AddHours(-24), TimeSpan.FromHours(1), "1h"),
        };

        var rows = await db.Executions
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.CreatedAt >= since)
            .Select(e => new { e.CreatedAt, e.Status })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var now = clock.UtcNow;
        var bucketSize = bucket;
        var firstBucket = AlignDown(since, bucketSize);
        var lastBucket = AlignDown(now, bucketSize);

        var byBucket = rows
            .GroupBy(r => AlignDown(r.CreatedAt, bucketSize))
            .ToDictionary(g => g.Key, g => g.ToList());

        var points = new List<ExecutionTimeseriesPoint>();
        for (var b = firstBucket; b <= lastBucket; b += bucketSize)
        {
            if (byBucket.TryGetValue(b, out var bucketRows))
            {
                points.Add(new ExecutionTimeseriesPoint(
                    b,
                    bucketRows.Count(r => r.Status == ExecutionStatus.Succeeded),
                    bucketRows.Count(r => r.Status == ExecutionStatus.Failed),
                    bucketRows.Count(r => r.Status == ExecutionStatus.Running),
                    bucketRows.Count(r => r.Status == ExecutionStatus.Pending)));
            }
            else
            {
                points.Add(new ExecutionTimeseriesPoint(b, 0, 0, 0, 0));
            }
        }

        return new ExecutionTimeseriesResponse(range == "7d" ? "7d" : "24h", label, points);
    }

    private static DateTimeOffset AlignDown(DateTimeOffset value, TimeSpan bucket)
    {
        var utc = value.UtcDateTime;
        var ticks = utc.Ticks - (utc.Ticks % bucket.Ticks);
        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }

    private static int Percentile(int[] sorted, double p)
    {
        if (sorted.Length == 1) return sorted[0];
        var rank = p * (sorted.Length - 1);
        var lower = (int)Math.Floor(rank);
        var upper = (int)Math.Ceiling(rank);
        if (lower == upper) return sorted[lower];
        var weight = rank - lower;
        return (int)Math.Round(sorted[lower] * (1 - weight) + sorted[upper] * weight);
    }
}
