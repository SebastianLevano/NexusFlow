using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusFlow.Integrations.Domain;

namespace NexusFlow.Integrations.Infrastructure.Configurations;

internal sealed class UserIntegrationConfiguration : IEntityTypeConfiguration<UserIntegration>
{
    public void Configure(EntityTypeBuilder<UserIntegration> builder)
    {
        builder.ToTable("int_user_integrations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Provider).IsRequired().HasConversion<int>();
        builder.Property(x => x.Label).IsRequired().HasMaxLength(80);
        builder.Property(x => x.CredentialsEncrypted).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.RevokedAt);
        builder.Property(x => x.LastUsedAt);

        builder.HasIndex(x => new { x.UserId, x.Provider });
        builder.HasIndex(x => new { x.UserId, x.RevokedAt });
    }
}
