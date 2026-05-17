using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace NexusFlow.Integrations.Infrastructure;

public static class IntegrationsAppExtensions
{
    public static async Task MigrateIntegrationsAsync(this WebApplication app, CancellationToken ct = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IntegrationsDbContext>();
        await db.Database.MigrateAsync(ct).ConfigureAwait(false);
    }
}
