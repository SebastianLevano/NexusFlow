namespace NexusFlow.Auth.Application.Security;

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);

public interface IJwtService
{
    AccessToken IssueAccessToken(Guid userId, string email);
}
