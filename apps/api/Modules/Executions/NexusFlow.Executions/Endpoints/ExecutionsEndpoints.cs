using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexusFlow.Executions.Application;
using NexusFlow.Shared.Web;

namespace NexusFlow.Executions.Endpoints;

internal static class ExecutionsEndpoints
{
    public static IEndpointRouteBuilder MapExecutions(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/executions")
            .WithTags("Executions")
            .RequireAuthorization();

        group.MapGet("/", List).WithName("ListExecutions");
        group.MapGet("/stats", Stats).WithName("ExecutionStats");
        group.MapGet("/timeseries", Timeseries).WithName("ExecutionTimeseries");
        group.MapGet("/{id:guid}", Get).WithName("GetExecution");

        return app;
    }

    public static IEndpointRouteBuilder MapManualTrigger(this IEndpointRouteBuilder app)
    {
        app.MapPost("/workflows/{id:guid}/runs", TriggerManual)
            .RequireAuthorization()
            .WithTags("Workflows")
            .WithName("TriggerWorkflowRun");
        return app;
    }

    public static IEndpointRouteBuilder MapWebhookTrigger(this IEndpointRouteBuilder app)
    {
        app.MapPost("/hooks/{workflowId:guid}/{secret}", TriggerWebhook)
            .AllowAnonymous()
            .WithTags("Webhooks")
            .WithName("TriggerWebhook");
        return app;
    }

    private static async Task<IResult> List(Guid? workflowId, string? status, string? range, ExecutionsService svc, CancellationToken ct)
        => (await svc.ListAsync(workflowId, status, range, ct).ConfigureAwait(false)).ToHttp();

    private static async Task<IResult> Get(Guid id, ExecutionsService svc, CancellationToken ct)
        => (await svc.GetAsync(id, ct).ConfigureAwait(false)).ToHttp();

    private static async Task<IResult> Stats(ExecutionsStatsService svc, CancellationToken ct)
        => (await svc.GetStatsAsync(ct).ConfigureAwait(false)).ToHttp();

    private static async Task<IResult> Timeseries(string? range, ExecutionsStatsService svc, CancellationToken ct)
        => (await svc.GetTimeseriesAsync(range ?? "24h", ct).ConfigureAwait(false)).ToHttp();

    private static async Task<IResult> TriggerManual(Guid id, JsonElement? payload, ExecutionsService svc, CancellationToken ct)
    {
        var result = await svc.TriggerManualAsync(id, payload, ct).ConfigureAwait(false);
        return result.ToHttp(executionId => Results.Accepted($"/executions/{executionId}", new { executionId }));
    }

    private static async Task<IResult> TriggerWebhook(Guid workflowId, string secret, JsonElement? payload, ExecutionsService svc, CancellationToken ct)
    {
        var result = await svc.TriggerWebhookAsync(workflowId, secret, payload, ct).ConfigureAwait(false);
        return result.ToHttp(executionId => Results.Accepted(value: new { executionId }));
    }
}
