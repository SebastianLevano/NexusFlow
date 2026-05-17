using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusFlow.Workflows.Domain;

namespace NexusFlow.Workflows.Infrastructure.Configurations;

internal sealed class WorkflowConfiguration : IEntityTypeConfiguration<Workflow>
{
    public void Configure(EntityTypeBuilder<Workflow> builder)
    {
        builder.ToTable("wf_workflows");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.UserId).IsRequired();

        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(w => w.Description)
            .HasMaxLength(2000);

        builder.Property(w => w.TriggerType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(w => w.TriggerConfig)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(w => w.IsActive).IsRequired();
        builder.Property(w => w.CreatedAt).IsRequired();
        builder.Property(w => w.UpdatedAt);

        builder.HasIndex(w => new { w.UserId, w.IsActive });
        builder.HasIndex(w => new { w.UserId, w.CreatedAt });

        builder.HasMany(w => w.Steps)
            .WithOne()
            .HasForeignKey(s => s.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Workflow.Steps))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
