using Api.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// Correlation-id behaviour, exercised through the real host. It needs no database (an unmatched
/// route still passes through the middleware), so it runs even without a test DB configured: every
/// response carries an X-Correlation-ID, and a client-supplied id is echoed back unchanged.
/// </summary>
[Trait("Category", "Integration")]
public class CorrelationIdTests
{
    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            b.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:Sql"] = "Server=unused;Database=unused;" })));

    [Fact]
    public async Task Every_response_carries_a_generated_correlation_id()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/does-not-exist");

        Assert.True(response.Headers.TryGetValues(CorrelationIdMiddleware.HeaderName, out var values));
        Assert.False(string.IsNullOrWhiteSpace(values!.First()));
    }

    [Fact]
    public async Task A_supplied_correlation_id_is_echoed_back()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/does-not-exist");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, "abc-123");
        var response = await client.SendAsync(request);

        Assert.Equal("abc-123", response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).First());
    }
}
