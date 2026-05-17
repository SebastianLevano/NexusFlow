using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace NexusFlow.Workflows.Infrastructure;

public static class WorkflowsMigrationExtensions
{
    public static async Task<WebApplication> MigrateWorkflowsAsync(this WebApplication app, CancellationToken ct = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkflowsDbContext>();
        await db.Database.MigrateAsync(ct).ConfigureAwait(false);
        return app;
    }
}
