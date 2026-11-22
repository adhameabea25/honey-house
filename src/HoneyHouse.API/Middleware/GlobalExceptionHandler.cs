using HoneyHouse.Application.Interfaces.Repositories;
using HoneyHouse.Application.Localization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HoneyHouse.API.Middleware;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    ILocalizer localizer,
    IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, code) = exception switch
        {
            ConcurrencyConflictException => (StatusCodes.Status409Conflict, "concurrency_conflict"),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "validation_failed"),
            OperationCanceledException when ct.IsCancellationRequested => (499, "server_error"),
            _ => (StatusCodes.Status500InternalServerError, "server_error")
        };

        if (status >= 500)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            logger.LogWarning(exception, "Handled exception {Code}", code);

        context.Response.StatusCode = status;
        var problem = new ProblemDetails { Status = status, Title = localizer[code] };
        problem.Extensions["code"] = code;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
