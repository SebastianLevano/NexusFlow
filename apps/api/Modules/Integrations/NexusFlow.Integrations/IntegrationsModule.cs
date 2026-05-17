using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexusFlow.Integrations.Abstractions;
using NexusFlow.Integrations.Application;
using NexusFlow.Integrations.Endpoints;
using NexusFlow.Integrations.Infrastructure;
using NexusFlow.Integrations.Providers;

namespace NexusFlow.Integrations;

public static class IntegrationsModule
{
    public const string Name = "integrations";

    public static IServiceCollection AddIntegrationsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Postgres.");

        services.AddDbContext<IntegrationsDbContext>(opt =>
        {
            opt.UseNpgsql(connectionString, npg => npg.MigrationsHistoryTable("__ef_migrations_history", "integrations"));
        });

        services.AddScoped<ICredentialProtector, DataProtectionCredentialProtector>();
        services.AddScoped<IntegrationsService>();
        services.AddScoped<IIntegrationCredentialsReader, IntegrationCredentialsReader>();

        services.AddSingleton<ISlackClient, SlackClient>();
        services.AddSingleton<IDiscordClient, DiscordClient>();
        services.AddHttpClient(SlackClient.HttpClientName, c => c.Timeout = TimeSpan.FromSeconds(10));
        services.AddHttpClient(DiscordClient.HttpClientName, c => c.Timeout = TimeSpan.FromSeconds(10));

        return services;
    }

    public static IEndpointRouteBuilder MapIntegrationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/integrations").WithTags("Integrations");

        group.MapGet("/health", () => Results.Ok(new { module = Name, status = "ok" }))
            .AllowAnonymous()
            .WithName("IntegrationsHealth");

        app.MapIntegrations();
        return app;
    }
}
