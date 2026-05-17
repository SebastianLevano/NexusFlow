using NexusFlow.Shared.Domain;

namespace NexusFlow.Integrations.Domain;

public sealed class UserIntegration : BaseEntity
{
    private UserIntegration() { }

    public Guid UserId { get; private set; }
    public IntegrationProvider Provider { get; private set; }
    public string Label { get; private set; } = default!;
    public string CredentialsEncrypted { get; private set; } = default!;
    public DateTimeOffset? RevokedAt { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }

    public static UserIntegration Create(
        Guid userId,
        IntegrationProvider provider,
        string label,
        string credentialsEncrypted,
        DateTimeOffset now)
    {
        return new UserIntegration
        {
            UserId = userId,
            Provider = provider,
            Label = label.Trim(),
            CredentialsEncrypted = credentialsEncrypted,
            CreatedAt = now,
        };
    }

    public void Rename(string label, DateTimeOffset now)
    {
        Label = label.Trim();
        UpdatedAt = now;
    }

    public void Rotate(string credentialsEncrypted, DateTimeOffset now)
    {
        CredentialsEncrypted = credentialsEncrypted;
        UpdatedAt = now;
    }

    public void Revoke(DateTimeOffset now)
    {
        RevokedAt = now;
        UpdatedAt = now;
    }

    public void MarkUsed(DateTimeOffset now)
    {
        LastUsedAt = now;
    }
}
