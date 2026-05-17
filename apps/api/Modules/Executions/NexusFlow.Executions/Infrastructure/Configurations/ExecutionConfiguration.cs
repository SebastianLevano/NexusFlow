using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusFlow.Executions.Domain;

namespace NexusFlow.Executions.Infrastructure.Configurations;

internal sealed class ExecutionConfiguration : IEntityTypeConfiguration<Execution>
{
    public void Configure(EntityTypeBuilder<Execution> builder)
    {
        builder.ToTable("exec_executions");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.WorkflowId).IsRequired();
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.Status).IsRequired().HasConversion<int>();
        builder.Property(e => e.TriggeredBy).IsRequired().HasConversion<int>();
        builder.Property(e => e.TriggerPayload).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.StartedAt);
        builder.Property(e => e.FinishedAt);
        builder.Property(e => e.DurationMs);
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);

        builder.HasIndex(e => new { e.UserId, e.CreatedAt });
        builder.HasIndex(e => new { e.WorkflowId, e.Status });
        builder.HasIndex(e => new { e.WorkflowId, e.CreatedAt });

        builder.HasMany(e => e.Steps)
            .WithOne()
            .HasForeignKey(s => s.ExecutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Execution.Steps))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
