using System.Net;
using System.Net.Http.Json;
using Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Tests.Integration.Infrastructure;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// Contract tests for the read endpoints — the real API hosted in-process (WebApplicationFactory)
/// over the seeded test database, pointed at it via configuration. Asserts status codes and the
/// JSON shape clients depend on (store count, detail incl. concurrency token, 404 for unknown).
/// </summary>
[Collection(SqlServerCollection.Name)]
[Trait("Category", "Integration")]
public class DistrictReadEndpointsTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sql;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public DistrictReadEndpointsTests(SqlServerFixture sql) => _sql = sql;

    public async Task InitializeAsync()
    {
        if (!_sql.IsAvailable)
            return;

        await _sql.ResetAsync();
        await _sql.ApplySeedAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            b.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:Sql"] = _sql.ConnectionString })));
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null)
            await _factory.DisposeAsync();
    }

    [SkippableFact]
    public async Task Get_districts_returns_all_with_store_counts()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");

        var districts = await _client!.GetFromJsonAsync<List<DistrictSummaryDto>>("/api/v1/districts");

        Assert.Equal(5, districts!.Count);
        Assert.Contains(districts, d => d.Name == "North Denmark" && d.StoreCount == 4);
        Assert.Contains(districts, d => d.Name == "Zealand" && d.StoreCount == 0);
    }

    [SkippableFact]
    public async Task Get_district_by_id_returns_detail_with_a_concurrency_token()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");

        var detail = await _client!.GetFromJsonAsync<DistrictDetailDto>("/api/v1/districts/3");

        Assert.Equal("Copenhagen", detail!.Name);
        Assert.Equal(3, detail.Secondaries.Count);
        Assert.Equal(5, detail.Stores.Count);
        Assert.False(string.IsNullOrWhiteSpace(detail.ConcurrencyToken));
    }

    [SkippableFact]
    public async Task Get_unknown_district_returns_404()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");

        var response = await _client!.GetAsync("/api/v1/districts/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [SkippableFact]
    public async Task Get_salespersons_returns_all()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");

        var people = await _client!.GetFromJsonAsync<List<SalespersonDto>>("/api/v1/salespersons");

        Assert.Equal(8, people!.Count);
    }
}
