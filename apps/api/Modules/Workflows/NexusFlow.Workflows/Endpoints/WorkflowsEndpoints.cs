using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexusFlow.Shared.Web;
using NexusFlow.Workflows.Application;

namespace NexusFlow.Workflows.Endpoints;

internal static class WorkflowsEndpoints
{
    public static IEndpointRouteBuilder MapWorkflows(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/workflows")
            .WithTags("Workflows")
            .RequireAuthorization();

        group.MapGet("/", List).WithName("ListWorkflows");
        group.MapGet("/{id:guid}", Get).WithName("GetWorkflow");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateWorkflowRequest>>()
            .WithName("CreateWorkflow");

        group.MapPut("/{id:guid}", Update)
            .AddEndpointFilter<ValidationFilter<UpdateWorkflowRequest>>()
            .WithName("UpdateWorkflow");

        group.MapDelete("/{id:guid}", Delete).WithName("DeleteWorkflow");

        group.MapPost("/{id:guid}/activate", Activate).WithName("ActivateWorkflow");
        group.MapPost("/{id:guid}/deactivate", Deactivate).WithName("DeactivateWorkflow");

        group.MapPut("/{id:guid}/layout", UpdateLayout).WithName("UpdateWorkflowLayout");

        return app;
    }

    private static async Task<IResult> List(WorkflowsService svc, CancellationToken ct)
        => (await svc.ListAsync(ct).ConfigureAwait(false)).ToHttp();

    private static async Task<IResult> Get(Guid id, WorkflowsService svc, CancellationToken ct)
        => (await svc.GetAsync(id, ct).ConfigureAwait(false)).ToHttp();

    private static async Task<IResult> Create(CreateWorkflowRequest req, WorkflowsService svc, CancellationToken ct)
        => (await svc.CreateAsync(req, ct).ConfigureAwait(false))
            .ToHttp(r => Results.Created($"/workflows/{r.Id}", r));

    private static async Task<IResult> Update(Guid id, UpdateWorkflowRequest req, WorkflowsService svc, CancellationToken ct)
        => (await svc.UpdateAsync(id, req, ct).ConfigureAwait(false)).ToHttp();

    private static async Task<IResult> Delete(Guid id, WorkflowsService svc, CancellationToken ct)
        => (await svc.DeleteAsync(id, ct).ConfigureAwait(false)).ToHttp();

    private static async Task<IResult> Activate(Guid id, WorkflowsService svc, CancellationToken ct)
        => (await svc.ActivateAsync(id, ct).ConfigureAwait(false)).ToHttp();

    private static async Task<IResult> Deactivate(Guid id, WorkflowsService svc, CancellationToken ct)
        => (await svc.DeactivateAsync(id, ct).ConfigureAwait(false)).ToHttp();

    private static async Task<IResult> UpdateLayout(Guid id, UpdateLayoutRequest req, WorkflowsService svc, CancellationToken ct)
        => (await svc.UpdateLayoutAsync(id, req, ct).ConfigureAwait(false)).ToHttp();
}
