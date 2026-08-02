using Api.Infrastructure;
using Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Tests.Unit.Api;

/// <summary>
/// The exception→ProblemDetails mapping: typed domain exceptions become their HTTP status, unknown
/// exceptions become a generic 500 that leaks nothing, and every response carries a traceId.
/// </summary>
[Trait("Category", "Unit")]
public class ProblemExceptionHandlerTests
{
    private async Task<(int status, ProblemDetails problem)> Handle(Exception exception)
    {
        ProblemDetailsContext? captured = null;
        var problemService = new Mock<IProblemDetailsService>();
        problemService
            .Setup(p => p.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .Callback<ProblemDetailsContext>(c => captured = c)
            .ReturnsAsync(true);

        var handler = new ProblemExceptionHandler(problemService.Object, NullLogger<ProblemExceptionHandler>.Instance);
        var httpContext = new DefaultHttpContext { TraceIdentifier = "trace-123" };

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(httpContext.Response.StatusCode, captured!.ProblemDetails.Status);
        return (httpContext.Response.StatusCode, captured.ProblemDetails);
    }

    [Fact]
    public async Task Not_found_maps_to_404()
    {
        var (status, _) = await Handle(new NotFoundException("district 9 not found"));
        Assert.Equal(404, status);
    }

    [Fact]
    public async Task Conflict_maps_to_409()
    {
        var (status, _) = await Handle(new ConflictException("already a secondary"));
        Assert.Equal(409, status);
    }

    [Fact]
    public async Task Concurrency_conflict_maps_to_409()
    {
        var (status, _) = await Handle(new ConcurrencyException("stale token"));
        Assert.Equal(409, status);
    }

    [Fact]
    public async Task Unknown_exceptions_become_a_generic_500_that_leaks_nothing()
    {
        var (status, problem) = await Handle(new InvalidOperationException("secret internal detail"));

        Assert.Equal(500, status);
        Assert.DoesNotContain("secret", problem.Detail);
    }

    [Fact]
    public async Task Every_response_carries_a_trace_id()
    {
        var (_, problem) = await Handle(new NotFoundException("nope"));

        Assert.True(problem.Extensions.TryGetValue("traceId", out var traceId));
        Assert.Equal("trace-123", traceId);
    }
}
