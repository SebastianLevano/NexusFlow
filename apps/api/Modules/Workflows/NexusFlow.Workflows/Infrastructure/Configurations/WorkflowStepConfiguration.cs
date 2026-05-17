using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusFlow.Workflows.Domain;

namespace NexusFlow.Workflows.Infrastructure.Configurations;

internal sealed class WorkflowStepConfiguration : IEntityTypeConfiguration<WorkflowStep>
{
    public void Configure(EntityTypeBuilder<WorkflowStep> builder)
    {
        builder.ToTable("wf_steps");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.WorkflowId).IsRequired();
        builder.Property(s => s.OrderIndex).IsRequired();

        builder.Property(s => s.ActionType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(s => s.Config)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt);

        builder.HasIndex(s => new { s.WorkflowId, s.OrderIndex }).IsUnique();
    }
}
