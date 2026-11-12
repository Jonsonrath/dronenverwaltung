using DroneInventory.Api.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DroneInventory.Api.Errors;

// Maps domain exceptions to RFC 7807 ProblemDetails. Title and detail are German: the UI shows them as they are.
// Any other exception is
// not handled here and stays a 500.
public class DomainExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Nicht gefunden"),
            ConflictException => (StatusCodes.Status409Conflict, "Konflikt"),
            ValidationException => (StatusCodes.Status422UnprocessableEntity, "Ungültige Eingabe"),
            _ => (0, null),
        };
        if (status == 0)
        {
            return false;
        }

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = exception.Message,
            },
        });
    }
}
