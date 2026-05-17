using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexusFlow.Auth.Infrastructure;
using Testcontainers.PostgreSql;

namespace NexusFlow.IntegrationTests;

public sealed class AuthFlowTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("nexusflow_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private WebApplicationFactory<Program> _factory = default!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync().ConfigureAwait(false);

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());
            builder.UseSetting("Jwt:Issuer", "nexusflow");
            builder.UseSetting("Jwt:Audience", "nexusflow-web");
            builder.UseSetting("Jwt:SigningKey", "test-signing-key-must-be-32-chars-or-more-please");
            builder.UseSetting("Jwt:AccessTokenMinutes", "15");
            builder.UseSetting("Jwt:RefreshTokenDays", "14");
        });

        // Trigger startup so migrations run
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await db.Database.MigrateAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync().ConfigureAwait(false);
        await _postgres.DisposeAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task Register_login_refresh_me_logout_full_flow()
    {
        var client = _factory.CreateClient();

        // Register
        var register = await client.PostAsJsonAsync("/auth/register", new
        {
            email = $"user-{Guid.NewGuid():N}@nexusflow.test",
            password = "Sup3rSecret!",
        }).ConfigureAwait(false);
        register.StatusCode.Should().Be(HttpStatusCode.Created);

        var auth = (await register.Content.ReadFromJsonAsync<AuthEnvelope>().ConfigureAwait(false))!;
        auth.Tokens.AccessToken.Should().NotBeNullOrWhiteSpace();
        auth.Tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();

        // Me with access token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Tokens.AccessToken);
        var me = await client.GetAsync("/auth/me").ConfigureAwait(false);
        me.StatusCode.Should().Be(HttpStatusCode.OK);

        // Refresh rotates the token
        var refresh = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = auth.Tokens.RefreshToken })
            .ConfigureAwait(false);
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotated = (await refresh.Content.ReadFromJsonAsync<AuthEnvelope>().ConfigureAwait(false))!;
        rotated.Tokens.RefreshToken.Should().NotBe(auth.Tokens.RefreshToken);

        // Old refresh token is now invalid
        var reuse = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = auth.Tokens.RefreshToken })
            .ConfigureAwait(false);
        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Logout revokes the new one
        var logout = await client.PostAsJsonAsync("/auth/logout", new { refreshToken = rotated.Tokens.RefreshToken })
            .ConfigureAwait(false);
        logout.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK);

        var afterLogout = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = rotated.Tokens.RefreshToken })
            .ConfigureAwait(false);
        afterLogout.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_with_bad_credentials_returns_401()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/auth/login", new
        {
            email = "missing@nexusflow.test",
            password = "Nothing12",
        }).ConfigureAwait(false);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_without_token_is_unauthorized()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/auth/me").ConfigureAwait(false);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record AuthEnvelope(Guid UserId, string Email, TokensEnvelope Tokens);
    private sealed record TokensEnvelope(
        string AccessToken,
        DateTimeOffset AccessTokenExpiresAt,
        string RefreshToken,
        DateTimeOffset RefreshTokenExpiresAt);
}
