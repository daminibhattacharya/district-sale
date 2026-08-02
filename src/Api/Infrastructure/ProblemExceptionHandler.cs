using System.Diagnostics;
using Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Infrastructure;

/// <summary>
/// Translates typed domain exceptions into RFC 9457 Problem Details responses:
/// <see cref="NotFoundException"/> → 404, <see cref="ConflictException"/> and
/// <see cref="ConcurrencyException"/> → 409, anything else → 500 with a generic message (stack
/// traces and internal detail are never leaked). Every response carries a <c>traceId</c> so a
/// report can be tied back to a log entry.
/// </summary>
public sealed class ProblemExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<ProblemExceptionHandler> _logger;

    public ProblemExceptionHandler(IProblemDetailsService problemDetails, ILogger<ProblemExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            ConcurrencyException => (StatusCodes.Status409Conflict, "Concurrency conflict"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        var isServerError = status == StatusCodes.Status500InternalServerError;
        if (isServerError)
            _logger.LogError(exception, "Unhandled exception processing {Path}", httpContext.Request.Path);
        else
            _logger.LogInformation("Request to {Path} rejected ({Status}): {Message}",
                httpContext.Request.Path, status, exception.Message);

        httpContext.Response.StatusCode = status;

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                // Domain messages are safe/business-facing; 500s stay generic to avoid leaking internals.
                Detail = isServerError ? "An unexpected error occurred." : exception.Message,
                Extensions = { ["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier }
            }
        });
    }
}
