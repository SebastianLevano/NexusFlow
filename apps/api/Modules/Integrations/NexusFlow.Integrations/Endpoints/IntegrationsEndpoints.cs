using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexusFlow.Integrations.Application;
using NexusFlow.Shared.Web;

namespace NexusFlow.Integrations.Endpoints;

internal static class IntegrationsEndpoints
{
    public static IEndpointRouteBuilder MapIntegrations(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/integrations")
            .WithTags("Integrations")
            .RequireAuthorization();

        group.MapGet("/", List).WithName("ListIntegrations");
        group.MapPost("/", Create).WithName("CreateIntegration");
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteIntegration");
        group.MapPost("/{id:guid}/test", Test).WithName("TestIntegration");

        return app;
    }

    private static async Task<IResult> List(IntegrationsService svc, CancellationToken ct)
        => (await svc.ListAsync(ct).ConfigureAwait(false)).ToHttp();

    private static async Task<IResult> Create(CreateIntegrationRequest req, IntegrationsService svc, CancellationToken ct)
    {
        var result = await svc.CreateAsync(req, ct).ConfigureAwait(false);
        return result.ToHttp(s => Results.Created($"/integrations/{s.Id}", s));
    }

    private static async Task<IResult> Delete(Guid id, IntegrationsService svc, CancellationToken ct)
    {
        var result = await svc.DeleteAsync(id, ct).ConfigureAwait(false);
        return result.ToHttp(_ => Results.NoContent());
    }

    private static async Task<IResult> Test(Guid id, TestMessageRequest req, IntegrationsService svc, CancellationToken ct)
        => (await svc.TestAsync(id, req, ct).ConfigureAwait(false)).ToHttp();
}
