using Dapper;
using Microsoft.Data.SqlClient;
using Tests.Integration.Infrastructure;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// BR-1 — every district has a name. Proven at the database level: each violation is rejected by
/// a constraint, not merely by the application. A district needs no primary yet at this stage of
/// the schema, so these tests insert on <c>Name</c> alone.
/// </summary>
[Collection(SqlServerCollection.Name)]
[Trait("Category", "Integration")]
public class DistrictNameRuleTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sql;

    public DistrictNameRuleTests(SqlServerFixture sql) => _sql = sql;

    public Task InitializeAsync() => _sql.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [SkippableFact]
    public async Task Inserting_a_district_with_a_null_name_is_rejected()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        await using var conn = await _sql.OpenConnectionAsync();

        await Assert.ThrowsAsync<SqlException>(() => conn.ExecuteAsync(
            "INSERT INTO dbo.District (Name) VALUES (@name);", new { name = (string?)null }));
    }

    [SkippableFact]
    public async Task Inserting_a_district_with_a_blank_name_is_rejected()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        await using var conn = await _sql.OpenConnectionAsync();

        await Assert.ThrowsAsync<SqlException>(() => conn.ExecuteAsync(
            "INSERT INTO dbo.District (Name) VALUES (@name);", new { name = "   " }));
    }

    [SkippableFact]
    public async Task Inserting_a_duplicate_district_name_is_rejected()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        await using var conn = await _sql.OpenConnectionAsync();

        await conn.ExecuteAsync("INSERT INTO dbo.District (Name) VALUES (@name);", new { name = "North Denmark" });

        var ex = await Assert.ThrowsAsync<SqlException>(() => conn.ExecuteAsync(
            "INSERT INTO dbo.District (Name) VALUES (@name);", new { name = "North Denmark" }));
        Assert.Equal(2627, ex.Number); // unique constraint violation
    }
}
