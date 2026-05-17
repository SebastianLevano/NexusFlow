using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace NexusFlow.Auth.Infrastructure;

public static class AuthMigrationExtensions
{
    public static async Task<WebApplication> MigrateAuthAsync(this WebApplication app, CancellationToken ct = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await db.Database.MigrateAsync(ct).ConfigureAwait(false);
        return app;
    }
}
