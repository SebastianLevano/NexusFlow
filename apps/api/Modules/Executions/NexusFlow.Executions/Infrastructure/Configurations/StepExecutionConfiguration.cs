using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusFlow.Executions.Domain;

namespace NexusFlow.Executions.Infrastructure.Configurations;

internal sealed class StepExecutionConfiguration : IEntityTypeConfiguration<StepExecution>
{
    public void Configure(EntityTypeBuilder<StepExecution> builder)
    {
        builder.ToTable("exec_step_executions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.ExecutionId).IsRequired();
        builder.Property(s => s.StepId).IsRequired();
        builder.Property(s => s.OrderIndex).IsRequired();
        builder.Property(s => s.ActionType).IsRequired().HasMaxLength(64);
        builder.Property(s => s.Status).IsRequired().HasConversion<int>();
        builder.Property(s => s.Input).IsRequired().HasColumnType("jsonb");
        builder.Property(s => s.Output).IsRequired().HasColumnType("jsonb");
        builder.Property(s => s.Error).HasMaxLength(4000);
        builder.Property(s => s.StartedAt);
        builder.Property(s => s.FinishedAt);
        builder.Property(s => s.DurationMs);
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt);

        builder.HasIndex(s => new { s.ExecutionId, s.OrderIndex });
    }
}
