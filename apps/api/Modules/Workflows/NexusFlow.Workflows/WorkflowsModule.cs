using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexusFlow.Shared.Web;
using NexusFlow.Workflows.Abstractions;
using NexusFlow.Workflows.Application;
using NexusFlow.Workflows.Endpoints;
using NexusFlow.Workflows.Infrastructure;

namespace NexusFlow.Workflows;

public static class WorkflowsModule
{
    public const string Name = "workflows";

    public static IServiceCollection AddWorkflowsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<WorkflowsDbContext>((sp, opt) =>
        {
            var cs = configuration.GetConnectionString("Postgres")
                     ?? throw new InvalidOperationException("Missing ConnectionStrings:Postgres.");
            opt.UseNpgsql(cs, npg => npg.MigrationsHistoryTable("__ef_migrations_history", "workflows"));
        });

        services.AddScoped<WorkflowsService>();
        services.AddScoped<IWorkflowReader, WorkflowReader>();

        services.AddSingleton<IValidator<CreateWorkflowRequest>, CreateWorkflowRequestValidator>();
        services.AddSingleton<IValidator<UpdateWorkflowRequest>, UpdateWorkflowRequestValidator>();
        services.AddScoped<ValidationFilter<CreateWorkflowRequest>>();
        services.AddScoped<ValidationFilter<UpdateWorkflowRequest>>();

        return services;
    }

    public static IEndpointRouteBuilder MapWorkflowsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/workflows").WithTags("Workflows");
        group.MapGet("/health", () => Results.Ok(new { module = Name, status = "ok" }))
            .AllowAnonymous()
            .WithName("WorkflowsHealth");

        app.MapWorkflows();
        return app;
    }
}
