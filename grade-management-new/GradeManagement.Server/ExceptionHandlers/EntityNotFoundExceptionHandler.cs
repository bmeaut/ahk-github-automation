using AutSoft.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.ExceptionHandlers;

public class EntityNotFoundExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is EntityNotFoundException entityNotFoundException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            httpContext.Response.ContentType = "application/json";
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Entity with given properties could not be found!",
                Detail = entityNotFoundException.Message
            };

            httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
        return false;
    }
}
