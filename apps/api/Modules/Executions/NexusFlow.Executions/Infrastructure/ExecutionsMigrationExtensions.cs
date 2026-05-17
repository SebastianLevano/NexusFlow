using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace NexusFlow.Executions.Infrastructure;

public static class ExecutionsMigrationExtensions
{
    public static async Task<WebApplication> MigrateExecutionsAsync(this WebApplication app, CancellationToken ct = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExecutionsDbContext>();
        await db.Database.MigrateAsync(ct).ConfigureAwait(false);
        return app;
    }
}
