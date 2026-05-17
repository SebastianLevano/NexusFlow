using Microsoft.EntityFrameworkCore;
using NexusFlow.Integrations.Domain;

namespace NexusFlow.Integrations.Infrastructure;

public sealed class IntegrationsDbContext(DbContextOptions<IntegrationsDbContext> options) : DbContext(options)
{
    public DbSet<UserIntegration> UserIntegrations => Set<UserIntegration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IntegrationsDbContext).Assembly);
    }
}
