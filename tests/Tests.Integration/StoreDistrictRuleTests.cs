using Dapper;
using Microsoft.Data.SqlClient;
using Tests.Integration.Infrastructure;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// BR-2 — every store belongs to exactly one district. Enforced by a <c>NOT NULL</c> foreign key,
/// with <c>ON DELETE CASCADE</c> so a district's stores go with it.
/// </summary>
[Collection(SqlServerCollection.Name)]
[Trait("Category", "Integration")]
public class StoreDistrictRuleTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sql;

    public StoreDistrictRuleTests(SqlServerFixture sql) => _sql = sql;

    public Task InitializeAsync() => _sql.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [SkippableFact]
    public async Task Inserting_a_store_without_a_district_is_rejected()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        await using var conn = await _sql.OpenConnectionAsync();

        await Assert.ThrowsAsync<SqlException>(() => conn.ExecuteAsync(
            "INSERT INTO dbo.Store (Name, DistrictId) VALUES (@name, @districtId);",
            new { name = "Aalborg Store", districtId = (int?)null }));
    }

    [SkippableFact]
    public async Task Inserting_a_store_for_a_nonexistent_district_is_rejected()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        await using var conn = await _sql.OpenConnectionAsync();

        var ex = await Assert.ThrowsAsync<SqlException>(() => conn.ExecuteAsync(
            "INSERT INTO dbo.Store (Name, DistrictId) VALUES (@name, @districtId);",
            new { name = "Ghost Store", districtId = 999_999 }));
        Assert.Equal(547, ex.Number); // foreign key violation
    }

    [SkippableFact]
    public async Task Deleting_a_district_cascades_its_stores()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        await using var conn = await _sql.OpenConnectionAsync();

        var districtId = await conn.QuerySingleAsync<int>(
            "INSERT INTO dbo.District (Name) VALUES (@name); SELECT CAST(SCOPE_IDENTITY() AS int);",
            new { name = "North Denmark" });
        await conn.ExecuteAsync(
            "INSERT INTO dbo.Store (Name, DistrictId) VALUES (@name, @districtId);",
            new { name = "Aalborg Store", districtId });

        await conn.ExecuteAsync("DELETE FROM dbo.District WHERE Id = @districtId;", new { districtId });

        var remaining = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM dbo.Store WHERE DistrictId = @districtId;", new { districtId });
        Assert.Equal(0, remaining);
    }
}
