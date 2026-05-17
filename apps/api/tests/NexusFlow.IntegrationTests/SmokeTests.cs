using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NexusFlow.IntegrationTests;

public sealed class SmokeTests : IClassFixture<SmokeTests.Factory>
{
    private readonly Factory _factory;
    public SmokeTests(Factory factory) => _factory = factory;

    [Fact]
    public async Task Root_returns_ok()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/").ConfigureAwait(false);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/auth/health")]
    [InlineData("/workflows/health")]
    [InlineData("/executions/health")]
    [InlineData("/integrations/health")]
    [InlineData("/health/live")]
    public async Task Module_health_endpoints_return_ok(string path)
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(path).ConfigureAwait(false);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Postgres", "Host=localhost;Port=1;Database=unused;Username=u;Password=p");
            builder.UseSetting("Jwt:Issuer", "nexusflow");
            builder.UseSetting("Jwt:Audience", "nexusflow-web");
            builder.UseSetting("Jwt:SigningKey", "smoke-tests-signing-key-32-chars-or-more-x");
            builder.UseSetting("Jwt:AccessTokenMinutes", "15");
            builder.UseSetting("Jwt:RefreshTokenDays", "14");
        }
    }
}
