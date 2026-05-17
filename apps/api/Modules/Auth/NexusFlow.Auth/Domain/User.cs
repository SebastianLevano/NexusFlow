using NexusFlow.Shared.Domain;

namespace NexusFlow.Auth.Domain;

public sealed class User : BaseEntity
{
    private User() { }

    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;

    private readonly List<RefreshToken> _refreshTokens = [];
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public static User Create(string email, string passwordHash, DateTimeOffset now)
    {
        return new User
        {
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            CreatedAt = now,
        };
    }

    public RefreshToken IssueRefreshToken(string tokenHash, DateTimeOffset expiresAt, DateTimeOffset now)
    {
        var token = RefreshToken.Create(Id, tokenHash, expiresAt, now);
        _refreshTokens.Add(token);
        return token;
    }
}
