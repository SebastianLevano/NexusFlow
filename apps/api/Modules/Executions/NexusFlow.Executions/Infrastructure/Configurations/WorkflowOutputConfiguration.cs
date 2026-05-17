using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusFlow.Executions.Domain;

namespace NexusFlow.Executions.Infrastructure.Configurations;

internal sealed class WorkflowOutputConfiguration : IEntityTypeConfiguration<WorkflowOutput>
{
    public void Configure(EntityTypeBuilder<WorkflowOutput> builder)
    {
        builder.ToTable("exec_workflow_outputs");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.WorkflowId).IsRequired();
        builder.Property(o => o.ExecutionId).IsRequired();
        builder.Property(o => o.Payload).IsRequired().HasColumnType("jsonb");
        builder.Property(o => o.CreatedAt).IsRequired();

        builder.HasIndex(o => new { o.WorkflowId, o.CreatedAt });
        builder.HasIndex(o => o.ExecutionId);
    }
}
