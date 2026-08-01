using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// The health endpoint, through the real host. Needs no database, so it runs even without a test DB.
/// Asserts 200 + "Healthy", and that the exposed timestamp is UTC — serialized with a trailing 'Z'
/// (the API exposes no local times).
/// </summary>
[Trait("Category", "Integration")]
public class HealthEndpointTests
{
    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            b.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:Sql"] = "Server=unused;Database=unused;" })));

    [Fact]
    public async Task Health_returns_200_and_a_utc_timestamp()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", json.RootElement.GetProperty("status").GetString());

        var timeUtc = json.RootElement.GetProperty("timeUtc").GetString();
        Assert.EndsWith("Z", timeUtc); // serialized as UTC
    }
}
