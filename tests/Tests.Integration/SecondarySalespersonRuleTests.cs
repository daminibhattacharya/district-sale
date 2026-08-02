using Dapper;
using Microsoft.Data.SqlClient;
using Tests.Integration.Infrastructure;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// BR-7 — a salesperson appears at most once as a secondary in a given district. Enforced by the
/// composite primary key on the link table. (The cross-table half of BR-7 — a person can't be both
/// primary and secondary of the same district — is enforced in the domain, since a CHECK cannot
/// span tables; see <c>DistrictTests.AddSecondary_rejects_the_current_primary</c>.)
/// </summary>
[Collection(SqlServerCollection.Name)]
[Trait("Category", "Integration")]
public class SecondarySalespersonRuleTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sql;

    public SecondarySalespersonRuleTests(SqlServerFixture sql) => _sql = sql;

    public Task InitializeAsync() => _sql.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [SkippableFact]
    public async Task Adding_the_same_secondary_twice_to_a_district_is_rejected()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        await using var conn = await _sql.OpenConnectionAsync();

        var primary = await conn.InsertSalespersonAsync("Alice");
        var district = await conn.InsertDistrictAsync("North Denmark", primary);
        var bob = await conn.InsertSalespersonAsync("Bob");
        await AddSecondaryAsync(conn, district, bob);

        var ex = await Assert.ThrowsAsync<SqlException>(() => AddSecondaryAsync(conn, district, bob));
        Assert.Equal(2627, ex.Number); // primary-key violation
    }

    [SkippableFact] // the composite key is per-district: the same person may be a secondary of several districts.
    public async Task The_same_salesperson_may_be_a_secondary_of_two_districts()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        await using var conn = await _sql.OpenConnectionAsync();

        var north = await conn.InsertDistrictAsync("North Denmark", await conn.InsertSalespersonAsync("Alice"));
        var south = await conn.InsertDistrictAsync("Southern Denmark", await conn.InsertSalespersonAsync("Carol"));
        var bob = await conn.InsertSalespersonAsync("Bob");

        await AddSecondaryAsync(conn, north, bob);
        await AddSecondaryAsync(conn, south, bob); // allowed — different district

        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM dbo.DistrictSecondarySalesperson WHERE SalespersonId = @bob;", new { bob });
        Assert.Equal(2, count);
    }

    private static Task AddSecondaryAsync(SqlConnection conn, int districtId, int salespersonId) =>
        conn.ExecuteAsync(
            "INSERT INTO dbo.DistrictSecondarySalesperson (DistrictId, SalespersonId) VALUES (@districtId, @salespersonId);",
            new { districtId, salespersonId });
}
