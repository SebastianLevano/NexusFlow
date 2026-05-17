using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexusFlow.Auth.Infrastructure;
using NexusFlow.Workflows.Infrastructure;
using Testcontainers.PostgreSql;

namespace NexusFlow.IntegrationTests;

public sealed class WorkflowsFlowTests : IAsyncLifetime
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
        });

        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.MigrateAsync().ConfigureAwait(false);
        await scope.ServiceProvider.GetRequiredService<WorkflowsDbContext>().Database.MigrateAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync().ConfigureAwait(false);
        await _postgres.DisposeAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task Authenticated_user_can_crud_and_toggle_workflow()
    {
        var client = await CreateAuthenticatedClientAsync().ConfigureAwait(false);

        var createBody = new
        {
            name = "Ping httpbin",
            description = "Smoke test",
            triggerType = "webhook",
            triggerConfig = new { secret = "abc" },
            steps = new object[]
            {
                new { orderIndex = 0, actionType = "http_request", config = new { method = "POST", url = "https://httpbin.org/post" } },
                new { orderIndex = 1, actionType = "save_to_database", config = new { } },
            },
        };

        var create = await client.PostAsJsonAsync("/workflows", createBody).ConfigureAwait(false);
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await create.Content.ReadFromJsonAsync<WorkflowEnvelope>().ConfigureAwait(false))!;
        created.Steps.Should().HaveCount(2);
        created.IsActive.Should().BeFalse();

        var list = await client.GetFromJsonAsync<WorkflowEnvelope[]>("/workflows").ConfigureAwait(false);
        list.Should().NotBeNull().And.HaveCount(1);

        var activate = await client.PostAsync($"/workflows/{created.Id}/activate", null).ConfigureAwait(false);
        activate.StatusCode.Should().Be(HttpStatusCode.OK);
        var activated = (await activate.Content.ReadFromJsonAsync<WorkflowEnvelope>().ConfigureAwait(false))!;
        activated.IsActive.Should().BeTrue();

        var del = await client.DeleteAsync($"/workflows/{created.Id}").ConfigureAwait(false);
        del.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK);

        var afterDelete = await client.GetAsync($"/workflows/{created.Id}").ConfigureAwait(false);
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Users_cannot_see_other_users_workflows()
    {
        var alice = await CreateAuthenticatedClientAsync().ConfigureAwait(false);
        var bob = await CreateAuthenticatedClientAsync().ConfigureAwait(false);

        var create = await alice.PostAsJsonAsync("/workflows", new
        {
            name = "Private",
            triggerType = "webhook",
            triggerConfig = new { },
            steps = Array.Empty<object>(),
        }).ConfigureAwait(false);
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var aliceWf = (await create.Content.ReadFromJsonAsync<WorkflowEnvelope>().ConfigureAwait(false))!;

        var bobList = await bob.GetFromJsonAsync<WorkflowEnvelope[]>("/workflows").ConfigureAwait(false);
        bobList.Should().BeEmpty();

        var bobAccess = await bob.GetAsync($"/workflows/{aliceWf.Id}").ConfigureAwait(false);
        bobAccess.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Anonymous_request_returns_401()
    {
        var resp = await _factory.CreateClient().GetAsync("/workflows").ConfigureAwait(false);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var register = await client.PostAsJsonAsync("/auth/register", new
        {
            email = $"u-{Guid.NewGuid():N}@nexusflow.test",
            password = "Sup3rSecret!",
        }).ConfigureAwait(false);
        register.EnsureSuccessStatusCode();

        var envelope = (await register.Content.ReadFromJsonAsync<AuthEnvelope>().ConfigureAwait(false))!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", envelope.Tokens.AccessToken);
        return client;
    }

    private sealed record AuthEnvelope(Guid UserId, string Email, TokensEnvelope Tokens);
    private sealed record TokensEnvelope(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt);

    private sealed record WorkflowEnvelope(
        Guid Id,
        string Name,
        string? Description,
        string TriggerType,
        JsonElement TriggerConfig,
        bool IsActive,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt,
        StepEnvelope[] Steps);

    private sealed record StepEnvelope(Guid Id, int OrderIndex, string ActionType, JsonElement Config);
}
