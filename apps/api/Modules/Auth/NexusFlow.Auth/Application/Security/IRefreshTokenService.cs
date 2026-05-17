namespace NexusFlow.Auth.Application.Security;

public sealed record RefreshTokenIssued(string Token, string TokenHash, DateTimeOffset ExpiresAt);

public interface IRefreshTokenService
{
    RefreshTokenIssued Issue();
    string Hash(string token);
}
