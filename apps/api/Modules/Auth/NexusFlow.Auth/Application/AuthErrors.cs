using NexusFlow.Shared.Results;

namespace NexusFlow.Auth.Application;

public static class AuthErrors
{
    public static readonly Error EmailAlreadyExists =
        Error.Conflict("auth.email_already_exists", "An account with this email already exists.");

    public static readonly Error InvalidCredentials =
        Error.Unauthorized("auth.invalid_credentials", "Invalid email or password.");

    public static readonly Error InvalidRefreshToken =
        Error.Unauthorized("auth.invalid_refresh_token", "Refresh token is invalid or expired.");

    public static readonly Error UserNotFound =
        Error.NotFound("auth.user_not_found", "User not found.");
}
