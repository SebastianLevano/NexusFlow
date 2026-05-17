namespace NexusFlow.Auth.Application;

public sealed record RegisterRequest(string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record LogoutRequest(string RefreshToken);

public sealed record AuthTokens(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

public sealed record AuthResponse(
    Guid UserId,
    string Email,
    AuthTokens Tokens);

public sealed record CurrentUserResponse(Guid UserId, string Email, DateTimeOffset CreatedAt);
