using Dapper;
using Microsoft.Data.SqlClient;
using Tests.Integration.Infrastructure;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// BR-1 — every district has a name. Proven at the database level: each violation is rejected by
/// a constraint, not merely by the application. A district also needs a primary (BR-4), so these
/// tests create a salesperson first and vary only the <c>Name</c>.
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
        var primaryId = await conn.InsertSalespersonAsync();

        await Assert.ThrowsAsync<SqlException>(() => conn.ExecuteAsync(
            "INSERT INTO dbo.District (Name, PrimarySalespersonId) VALUES (@name, @primaryId);",
            new { name = (string?)null, primaryId }));
    }

    [SkippableFact]
    public async Task Inserting_a_district_with_a_blank_name_is_rejected()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        await using var conn = await _sql.OpenConnectionAsync();
        var primaryId = await conn.InsertSalespersonAsync();

        await Assert.ThrowsAsync<SqlException>(() => conn.ExecuteAsync(
            "INSERT INTO dbo.District (Name, PrimarySalespersonId) VALUES (@name, @primaryId);",
            new { name = "   ", primaryId }));
    }

    [SkippableFact]
    public async Task Inserting_a_duplicate_district_name_is_rejected()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        await using var conn = await _sql.OpenConnectionAsync();
        var primaryId = await conn.InsertSalespersonAsync();
        await conn.InsertDistrictAsync("North Denmark", primaryId);

        var ex = await Assert.ThrowsAsync<SqlException>(() => conn.ExecuteAsync(
            "INSERT INTO dbo.District (Name, PrimarySalespersonId) VALUES (@name, @primaryId);",
            new { name = "North Denmark", primaryId }));
        Assert.Equal(2627, ex.Number); // unique constraint violation
    }
}
