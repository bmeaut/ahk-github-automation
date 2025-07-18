using Ahk.GradeManagement.Backend.Common.Options;
using Ahk.GradeManagement.Shared.Dtos.ErrorHandling;

using AutSoft.Common.Exceptions;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ahk.GradeManagement.Api.ErrorHandling;

public static class ErrorHandlingExtensions
{
    public static IServiceCollection AddCustomProblemDetails(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureOption<ErrorHandlingOptions>(configuration);

        return services.AddProblemDetails(o => o.CustomizeProblemDetails = (ctx) =>
        {
            switch (ctx.HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error)
            {
                case EntityNotFoundException e:
                    UpdateProblemDetails(ctx, e, StatusCodes.Status404NotFound, "Entity not found!", detail: e.Message);
                    break;
                case ForbiddenException e:
                    UpdateProblemDetails(ctx, e, StatusCodes.Status403Forbidden, e.Title);
                    break;
                case ValidationException e:
                    UpdateValidationProblemDetails(ctx, e, e.Errors, StatusCodes.Status400BadRequest, e.Title, e.Type?.ToString());
                    break;
                case BusinessException e:
                    UpdateProblemDetails(ctx, e, StatusCodes.Status500InternalServerError, e.Title, e.Type?.ToString(), e.Message);
                    break;
                // Unique constraints
                case DbUpdateException e when e.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627):
                    UpdateProblemDetails(ctx, e, StatusCodes.Status409Conflict, "Operation would conflicted to another object!");
                    break;
                case NotImplementedException e:
                    UpdateProblemDetails(ctx, e, StatusCodes.Status501NotImplemented, "Operation is not implemented yet!");
                    break;
                case OperationCanceledException e when ctx.HttpContext.RequestAborted.IsCancellationRequested:
                    // https://httpstatuses.com/499
                    UpdateProblemDetails(ctx, e, 499, "Operation was canceled!");
                    break;
                case OperationCanceledException e when !ctx.HttpContext.RequestAborted.IsCancellationRequested:
                    UpdateProblemDetails(ctx, e, StatusCodes.Status504GatewayTimeout, "Operation has benn timed out!");
                    break;
                case TimeoutException e:
                    UpdateProblemDetails(ctx, e, StatusCodes.Status504GatewayTimeout, "Operation has benn timed out!");
                    break;
                case HttpRequestException e when e.IsDbTimeout() || e.IsHttpTimeout():
                    UpdateProblemDetails(ctx, e, StatusCodes.Status504GatewayTimeout, "Operation has benn timed out!");
                    break;
                case Exception e:
                    UpdateProblemDetails(ctx, e, StatusCodes.Status500InternalServerError, "Error!");
                    break;
                default:
                    break;
            }
        });
    }

    public static void UpdateProblemDetails(
        ProblemDetailsContext ctx,
        Exception exception,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null)
    {
        if (statusCode.HasValue)
        {
            ctx.HttpContext.Response.StatusCode = statusCode.Value;
            ctx.ProblemDetails.Status = statusCode.Value;
        }

        if (title != null)
        {
            ctx.ProblemDetails.Title = title;
        }

        if (type != null)
        {
            ctx.ProblemDetails.Type = type;
        }

        if (detail != null)
        {
            ctx.ProblemDetails.Detail = detail;
        }

        if (instance != null)
        {
            ctx.ProblemDetails.Instance = instance;
        }

        var options = ctx.HttpContext.RequestServices.GetRequiredService<IOptions<ErrorHandlingOptions>>();

        if (options.Value.ReturnExceptionDetails)
        {
            ctx.ProblemDetails.Extensions["exception"] = new ExceptionDetails()
            {
                Message = exception.Message,
                Type = exception.GetType().Name,
                StackTrace = exception.StackTrace,
            };
        }
    }

    public static void UpdateValidationProblemDetails(
        this ProblemDetailsContext context,
        Exception exception,
        Dictionary<string, string> errors,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null)
    {
        UpdateProblemDetails(context, exception, statusCode, title, type, detail, instance);
        if (context.ProblemDetails is ValidationProblemDetails validationProblemDetails)
        {
            foreach (var error in errors)
            {
                validationProblemDetails.Errors[error.Key] = [error.Value];
            }
        }
        else if (context.ProblemDetails is not null)
        {
            var problemDetailsErrors = new Dictionary<string, string[]>();
            foreach (var error in errors)
            {
                problemDetailsErrors[error.Key] = [error.Value];
            }

            context.ProblemDetails.Extensions["errors"] = problemDetailsErrors;
        }
    }
}
