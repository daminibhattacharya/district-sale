using Dapper;
using Tests.Integration.Infrastructure;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// Optimistic-concurrency token. District carries a <c>rowversion</c> that SQL Server stamps on
/// insert and bumps on every update — the value later compared in <c>UPDATE … WHERE RowVersion=@v</c>
/// so two editors can't silently overwrite each other (the reject path is tested at the repo/API tier).
/// </summary>
[Collection(SqlServerCollection.Name)]
[Trait("Category", "Integration")]
public class DistrictConcurrencyTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sql;

    public DistrictConcurrencyTests(SqlServerFixture sql) => _sql = sql;

    public Task InitializeAsync() => _sql.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [SkippableFact]
    public async Task RowVersion_changes_when_a_district_is_updated()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        await using var conn = await _sql.OpenConnectionAsync();

        var primary = await conn.InsertSalespersonAsync("Alice");
        var district = await conn.InsertDistrictAsync("North Denmark", primary);

        var before = await conn.ExecuteScalarAsync<byte[]>(
            "SELECT RowVersion FROM dbo.District WHERE Id = @id;", new { id = district });
        await conn.ExecuteAsync(
            "UPDATE dbo.District SET Name = @name WHERE Id = @id;", new { name = "North", id = district });
        var after = await conn.ExecuteScalarAsync<byte[]>(
            "SELECT RowVersion FROM dbo.District WHERE Id = @id;", new { id = district });

        Assert.NotNull(before);
        Assert.False(before!.SequenceEqual(after!)); // the token moved
    }
}
