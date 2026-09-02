using CentroDeProduccion.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace CentroDeProduccion.Api.Extensions;

/// <summary>
/// The single place in the solution that maps a failed <see cref="Result"/> to an RFC 7807
/// ProblemDetails HTTP response (see design D5). Controllers call
/// <c>result.ToActionResult(controller, onSuccess)</c> instead of branching on error types
/// themselves.
/// </summary>
public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result, ControllerBase controller)
    {
        return result.IsSuccess
            ? controller.NoContent()
            : Problem(result.Error, controller);
    }

    public static IActionResult ToActionResult<TValue>(
        this Result<TValue> result,
        ControllerBase controller,
        Func<TValue, IActionResult>? onSuccess = null)
    {
        if (result.IsFailure)
        {
            return Problem(result.Error, controller);
        }

        return onSuccess is null ? controller.Ok(result.Value) : onSuccess(result.Value);
    }

    private static IActionResult Problem(Error error, ControllerBase controller)
    {
        var statusCode = ToStatusCode(error.Type);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = error.Type.ToString(),
            Detail = error.Type == ErrorType.Unexpected ? null : error.Message,
            Extensions =
            {
                ["errorCode"] = error.Code
            }
        };

        return controller.StatusCode(statusCode, problemDetails);
    }

    private static int ToStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Concurrency => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };
}
