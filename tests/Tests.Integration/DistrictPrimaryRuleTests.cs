using Dapper;
using Microsoft.Data.SqlClient;
using Tests.Integration.Infrastructure;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// BR-4 — a district always has exactly one primary salesperson (never zero, never two).
/// Modelled as a <c>NOT NULL</c> foreign-key column on District, which makes it a pure constraint:
/// a district cannot exist without a primary, the primary must be a real salesperson, and there is
/// exactly one because it is a single column — no trigger required.
/// </summary>
[Collection(SqlServerCollection.Name)]
[Trait("Category", "Integration")]
public class DistrictPrimaryRuleTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sql;

    public DistrictPrimaryRuleTests(SqlServerFixture sql) => _sql = sql;

    public Task InitializeAsync() => _sql.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [SkippableFact]
    public async Task Inserting_a_district_without_a_primary_is_rejected()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        await using var conn = await _sql.OpenConnectionAsync();

        await Assert.ThrowsAsync<SqlException>(() => conn.ExecuteAsync(
            "INSERT INTO dbo.District (Name) VALUES (@name);", new { name = "North Denmark" }));
    }

    [SkippableFact]
    public async Task Inserting_a_district_with_a_nonexistent_primary_is_rejected()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        await using var conn = await _sql.OpenConnectionAsync();

        var ex = await Assert.ThrowsAsync<SqlException>(() => conn.ExecuteAsync(
            "INSERT INTO dbo.District (Name, PrimarySalespersonId) VALUES (@name, @primaryId);",
            new { name = "North Denmark", primaryId = 999_999 }));
        Assert.Equal(547, ex.Number); // foreign key violation
    }

    [SkippableFact]
    public async Task A_district_with_a_real_primary_is_accepted()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        await using var conn = await _sql.OpenConnectionAsync();

        var primaryId = await conn.InsertSalespersonAsync("Alice");
        var districtId = await conn.InsertDistrictAsync("North Denmark", primaryId);

        Assert.True(districtId > 0);
    }

    [SkippableFact] // BR-6: the same salesperson may be primary of several districts (staff shortages).
    public async Task The_same_salesperson_may_be_primary_of_two_districts()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        await using var conn = await _sql.OpenConnectionAsync();

        var alice = await conn.InsertSalespersonAsync("Alice");
        await conn.InsertDistrictAsync("North Denmark", alice);
        await conn.InsertDistrictAsync("Southern Denmark", alice); // must be allowed — no UNIQUE on the column

        var districtsLedByAlice = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM dbo.District WHERE PrimarySalespersonId = @alice;", new { alice });
        Assert.Equal(2, districtsLedByAlice);
    }
}
