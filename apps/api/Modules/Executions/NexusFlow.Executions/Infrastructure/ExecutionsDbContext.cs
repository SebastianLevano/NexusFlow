using Microsoft.EntityFrameworkCore;
using NexusFlow.Executions.Domain;

namespace NexusFlow.Executions.Infrastructure;

public sealed class ExecutionsDbContext(DbContextOptions<ExecutionsDbContext> options) : DbContext(options)
{
    public DbSet<Execution> Executions => Set<Execution>();
    public DbSet<StepExecution> StepExecutions => Set<StepExecution>();
    public DbSet<WorkflowOutput> WorkflowOutputs => Set<WorkflowOutput>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExecutionsDbContext).Assembly);
    }
}
