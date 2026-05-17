using FluentValidation;
using Microsoft.AspNetCore.Http;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace NexusFlow.Shared.Web;

public sealed class ValidationFilter<T> : IEndpointFilter where T : notnull
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService(typeof(IValidator<T>)) as IValidator<T>;
        if (validator is null) return await next(context).ConfigureAwait(false);

        var arg = context.Arguments.OfType<T>().FirstOrDefault();
        if (arg is null) return await next(context).ConfigureAwait(false);

        var result = await validator.ValidateAsync(arg, context.HttpContext.RequestAborted).ConfigureAwait(false);
        if (result.IsValid) return await next(context).ConfigureAwait(false);

        var errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return HttpResults.ValidationProblem(errors);
    }
}
