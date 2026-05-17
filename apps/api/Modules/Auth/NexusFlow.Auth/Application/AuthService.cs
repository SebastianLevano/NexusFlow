using Microsoft.EntityFrameworkCore;
using NexusFlow.Auth.Application.Security;
using NexusFlow.Auth.Domain;
using NexusFlow.Auth.Infrastructure;
using NexusFlow.Shared.Results;
using NexusFlow.Shared.Time;

namespace NexusFlow.Auth.Application;

public sealed class AuthService(
    AuthDbContext db,
    IPasswordHasher hasher,
    IJwtService jwt,
    IRefreshTokenService refreshTokens,
    IClock clock)
{
    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var exists = await db.Users.AnyAsync(u => u.Email == email, ct).ConfigureAwait(false);
        if (exists) return AuthErrors.EmailAlreadyExists;

        var user = User.Create(email, hasher.Hash(request.Password), clock.UtcNow);
        db.Users.Add(user);

        var tokens = IssueTokens(user);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new AuthResponse(user.Id, user.Email, tokens);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email, ct).ConfigureAwait(false);
        if (user is null || !hasher.Verify(request.Password, user.PasswordHash))
            return AuthErrors.InvalidCredentials;

        var tokens = IssueTokens(user);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new AuthResponse(user.Id, user.Email, tokens);
    }

    public async Task<Result<AuthResponse>> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        var tokenHash = refreshTokens.Hash(request.RefreshToken);
        var existing = await db.RefreshTokens
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, ct)
            .ConfigureAwait(false);

        if (existing is null || !existing.IsActive(clock.UtcNow))
            return AuthErrors.InvalidRefreshToken;

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == existing.UserId, ct).ConfigureAwait(false);
        if (user is null) return AuthErrors.InvalidRefreshToken;

        var issued = refreshTokens.Issue();
        var newToken = user.IssueRefreshToken(issued.TokenHash, issued.ExpiresAt, clock.UtcNow);
        db.RefreshTokens.Add(newToken);
        existing.Revoke(clock.UtcNow, newToken.Id);

        var access = jwt.IssueAccessToken(user.Id, user.Email);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        var tokens = new AuthTokens(access.Token, access.ExpiresAt, issued.Token, issued.ExpiresAt);
        return new AuthResponse(user.Id, user.Email, tokens);
    }

    public async Task<Result> LogoutAsync(LogoutRequest request, CancellationToken ct)
    {
        var tokenHash = refreshTokens.Hash(request.RefreshToken);
        var existing = await db.RefreshTokens
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, ct)
            .ConfigureAwait(false);

        if (existing is null) return Result.Success();

        existing.Revoke(clock.UtcNow);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result<CurrentUserResponse>> GetCurrentAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, ct).ConfigureAwait(false);
        if (user is null) return AuthErrors.UserNotFound;
        return new CurrentUserResponse(user.Id, user.Email, user.CreatedAt);
    }

    private AuthTokens IssueTokens(User user)
    {
        var access = jwt.IssueAccessToken(user.Id, user.Email);
        var issued = refreshTokens.Issue();
        var token = user.IssueRefreshToken(issued.TokenHash, issued.ExpiresAt, clock.UtcNow);
        db.RefreshTokens.Add(token);
        return new AuthTokens(access.Token, access.ExpiresAt, issued.Token, issued.ExpiresAt);
    }
}
