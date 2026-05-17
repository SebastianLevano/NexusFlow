using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexusFlow.Auth.Abstractions;
using NexusFlow.Auth.Application;
using NexusFlow.Shared.Web;

namespace NexusFlow.Auth.Endpoints;

internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuth(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/register", Register)
            .AllowAnonymous()
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>()
            .WithName("Register");

        group.MapPost("/login", Login)
            .AllowAnonymous()
            .AddEndpointFilter<ValidationFilter<LoginRequest>>()
            .WithName("Login");

        group.MapPost("/refresh", Refresh)
            .AllowAnonymous()
            .AddEndpointFilter<ValidationFilter<RefreshRequest>>()
            .WithName("Refresh");

        group.MapPost("/logout", Logout)
            .AllowAnonymous()
            .AddEndpointFilter<ValidationFilter<LogoutRequest>>()
            .WithName("Logout");

        group.MapGet("/me", Me)
            .RequireAuthorization()
            .WithName("Me");

        group.MapGet("/health", () => Results.Ok(new { module = "auth", status = "ok" }))
            .AllowAnonymous()
            .WithName("AuthHealth");

        return app;
    }

    private static async Task<IResult> Register(RegisterRequest req, AuthService svc, CancellationToken ct)
        => (await svc.RegisterAsync(req, ct).ConfigureAwait(false)).ToHttp(r => Results.Created($"/auth/users/{r.UserId}", r));

    private static async Task<IResult> Login(LoginRequest req, AuthService svc, CancellationToken ct)
        => (await svc.LoginAsync(req, ct).ConfigureAwait(false)).ToHttp();

    private static async Task<IResult> Refresh(RefreshRequest req, AuthService svc, CancellationToken ct)
        => (await svc.RefreshAsync(req, ct).ConfigureAwait(false)).ToHttp();

    private static async Task<IResult> Logout(LogoutRequest req, AuthService svc, CancellationToken ct)
        => (await svc.LogoutAsync(req, ct).ConfigureAwait(false)).ToHttp();

    [Authorize]
    private static async Task<IResult> Me(ICurrentUser current, AuthService svc, CancellationToken ct)
    {
        if (current.UserId is not { } id) return Results.Unauthorized();
        return (await svc.GetCurrentAsync(id, ct).ConfigureAwait(false)).ToHttp();
    }
}
