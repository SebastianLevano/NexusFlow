using Microsoft.AspNetCore.Http;
using NexusFlow.Shared.Results;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace NexusFlow.Shared.Web;

public static class ResultExtensions
{
    public static IResult ToHttp<T>(this Result<T> result, Func<T, IResult>? onSuccess = null)
    {
        if (result.IsSuccess)
            return onSuccess is null ? HttpResults.Ok(result.Value) : onSuccess(result.Value);
        return MapError(result.Error);
    }

    public static IResult ToHttp(this Result result)
    {
        return result.IsSuccess ? HttpResults.NoContent() : MapError(result.Error);
    }

    private static IResult MapError(Error error)
    {
        var (status, title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Validation failed"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not found"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden"),
            _ => (StatusCodes.Status400BadRequest, "Request failed"),
        };

        return HttpResults.Problem(
            type: $"https://nexusflow.dev/errors/{error.Code}",
            title: title,
            detail: error.Message,
            statusCode: status,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
