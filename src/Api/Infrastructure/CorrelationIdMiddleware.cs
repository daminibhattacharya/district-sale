using Serilog.Context;

namespace Api.Infrastructure;

/// <summary>
/// Gives every request a correlation id: honours an incoming <c>X-Correlation-ID</c> header or mints
/// one, echoes it on the response, aligns <see cref="HttpContext.TraceIdentifier"/> to it (so the
/// ProblemDetails traceId matches), and pushes it onto the Serilog log context so every log line for
/// the request — including the request-completion log — carries it.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId =
            context.Request.Headers.TryGetValue(HeaderName, out var provided) && !string.IsNullOrWhiteSpace(provided)
                ? provided.ToString()
                : Guid.NewGuid().ToString();

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
