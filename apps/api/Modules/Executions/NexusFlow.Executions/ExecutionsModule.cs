using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusFlow.Executions.Abstractions;
using NexusFlow.Executions.Application;
using NexusFlow.Executions.Application.Actions;
using NexusFlow.Executions.Endpoints;
using NexusFlow.Executions.Infrastructure;
using NexusFlow.Executions.Realtime;

namespace NexusFlow.Executions;

public static class ExecutionsModule
{
    public const string Name = "executions";

    public static IServiceCollection AddExecutionsModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Postgres.");

        var isTesting = environment.IsEnvironment("Testing");

        services.AddDbContext<ExecutionsDbContext>(opt =>
        {
            opt.UseNpgsql(connectionString, npg => npg.MigrationsHistoryTable("__ef_migrations_history", "executions"));
        });

        services.AddScoped<ExecutionsService>();
        services.AddScoped<ExecutionsStatsService>();
        services.AddScoped<WorkflowExecutor>();

        services.AddScoped<IActionHandler, HttpRequestAction>();
        services.AddScoped<IActionHandler, SaveToDatabaseAction>();
        services.AddScoped<IActionHandler, SendNotificationAction>();
        services.AddScoped<IActionHandler, SlackPostMessageAction>();
        services.AddScoped<IActionHandler, DiscordPostMessageAction>();
        services.AddScoped<ActionHandlerResolver>();

        services.AddHttpClient("nexusflow-action-http");

        services.AddSingleton<IExecutionScheduler, HangfireExecutionScheduler>();
        services.AddSingleton<IScheduleRegistrar, HangfireScheduleRegistrar>();

        services.AddSignalR();
        services.AddSingleton<IExecutionLiveBus, SignalRExecutionLiveBus>();

        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(opt => opt.UseNpgsqlConnection(connectionString)));

        if (!isTesting)
        {
            services.AddHangfireServer(opt =>
            {
                opt.WorkerCount = Math.Max(2, Environment.ProcessorCount);
                opt.Queues = ["default"];
            });
        }

        return services;
    }

    public static IEndpointRouteBuilder MapExecutionsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/executions").WithTags("Executions");
        group.MapGet("/health", () => Results.Ok(new { module = Name, status = "ok" }))
            .AllowAnonymous()
            .WithName("ExecutionsHealth");

        app.MapExecutions();
        app.MapManualTrigger();
        app.MapWebhookTrigger();
        app.MapHub<ExecutionHub>("/hubs/executions");
        return app;
    }
}
