using System.Net;
using System.Net.Http.Json;
using Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Tests.Integration.Infrastructure;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// Contract tests for the ★ write path — <c>PUT /api/v1/districts/{id}/salespersons</c>. Covers the
/// full-list replace (change primary, and drop a secondary by omitting it), the no-primary rejection
/// (400), and optimistic concurrency (a stale token → 409), against the real API over the seeded DB.
/// </summary>
[Collection(SqlServerCollection.Name)]
[Trait("Category", "Integration")]
public class DistrictAssignmentsEndpointTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sql;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public DistrictAssignmentsEndpointTests(SqlServerFixture sql) => _sql = sql;

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

    private async Task<DistrictDetailDto> GetDetail(int id) =>
        (await _client!.GetFromJsonAsync<DistrictDetailDto>($"/api/v1/districts/{id}"))!;

    [SkippableFact]
    public async Task Put_replaces_the_primary_and_trims_the_secondary_list()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");

        var before = await GetDetail(3); // Copenhagen: primary Camilla (3), secondaries 2, 6, 7
        // Promote Freja (6) to primary; keep only Bjørn (2) as secondary (6 and 7 are dropped).
        var request = new PutAssignmentsRequest(PrimaryId: 6, SecondaryIds: new[] { 2 }, ConcurrencyToken: before.ConcurrencyToken);

        var response = await _client!.PutAsJsonAsync("/api/v1/districts/3/salespersons", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var after = (await response.Content.ReadFromJsonAsync<DistrictDetailDto>())!;
        Assert.Equal(6, after.Primary.Id);
        Assert.Equal(new[] { 2 }, after.Secondaries.Select(s => s.Id));
        Assert.NotEqual(before.ConcurrencyToken, after.ConcurrencyToken);
    }

    [SkippableFact]
    public async Task Put_with_no_primary_is_rejected_with_400()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");

        var before = await GetDetail(3);
        // PrimaryId 0 is not a valid salesperson id — a district can never be left without a primary.
        var request = new PutAssignmentsRequest(PrimaryId: 0, SecondaryIds: new[] { 2 }, ConcurrencyToken: before.ConcurrencyToken);

        var response = await _client!.PutAsJsonAsync("/api/v1/districts/3/salespersons", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [SkippableFact]
    public async Task Put_with_a_stale_token_is_rejected_with_409()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");

        var before = await GetDetail(3);
        var staleToken = before.ConcurrencyToken;

        // A first successful PUT advances the district's rowversion, making staleToken out of date.
        var first = new PutAssignmentsRequest(PrimaryId: 3, SecondaryIds: new[] { 2 }, ConcurrencyToken: staleToken);
        var firstResponse = await _client!.PutAsJsonAsync("/api/v1/districts/3/salespersons", first);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var stale = new PutAssignmentsRequest(PrimaryId: 3, SecondaryIds: Array.Empty<int>(), ConcurrencyToken: staleToken);
        var staleResponse = await _client!.PutAsJsonAsync("/api/v1/districts/3/salespersons", stale);

        Assert.Equal(HttpStatusCode.Conflict, staleResponse.StatusCode);
    }
}
