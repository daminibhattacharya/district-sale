using Dapper;
using DataAccess;
using Domain;
using Tests.Integration.Infrastructure;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// <see cref="DistrictRepository.ReplaceAssignmentsAsync"/> — the PUT full-list replace with an
/// optimistic-concurrency gate. Covers the happy path (primary swapped, secondary set replaced
/// wholesale), a stale rowversion token (rejected), and a primary that doesn't exist (rejected).
/// </summary>
[Collection(SqlServerCollection.Name)]
[Trait("Category", "Integration")]
public class DistrictRepositoryReplaceAssignmentsTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sql;

    public DistrictRepositoryReplaceAssignmentsTests(SqlServerFixture sql) => _sql = sql;

    public async Task InitializeAsync()
    {
        if (!_sql.IsAvailable)
            return;
        await _sql.ResetAsync();
        await _sql.ApplySeedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private DistrictRepository NewRepository() =>
        new(new SqlConnectionFactory(_sql.ConnectionString!));

    private async Task<byte[]> RowVersionOf(int districtId)
    {
        await using var conn = await _sql.OpenConnectionAsync();
        return (await conn.ExecuteScalarAsync<byte[]>(
            "SELECT RowVersion FROM dbo.District WHERE Id = @districtId;", new { districtId }))!;
    }

    [SkippableFact]
    public async Task Replaces_the_primary_and_the_whole_secondary_set()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        var repo = NewRepository();
        var token = await RowVersionOf(3); // Copenhagen: primary Camilla (3), secondaries 2,6,7

        // Promote Freja (6) to primary; secondaries become exactly Bjørn (2) + Helle (8).
        var newToken = await repo.ReplaceAssignmentsAsync(
            districtId: 3, primaryId: 6, secondaryIds: new[] { 2, 8 }, rowVersion: token);

        var copenhagen = await repo.GetByIdAsync(3);
        Assert.Equal(6, copenhagen!.Primary.Id);
        Assert.Equal(new[] { 2, 8 }, copenhagen.Secondaries.Select(s => s.Id).OrderBy(id => id));
        Assert.False(token.SequenceEqual(newToken)); // the rowversion advanced
    }

    [SkippableFact]
    public async Task Rejects_a_stale_rowversion_token()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        var repo = NewRepository();
        var staleToken = await RowVersionOf(3);

        // A first successful replace advances the rowversion, making staleToken out of date.
        await repo.ReplaceAssignmentsAsync(3, primaryId: 3, secondaryIds: new[] { 2 }, rowVersion: staleToken);

        await Assert.ThrowsAsync<ConcurrencyException>(() =>
            repo.ReplaceAssignmentsAsync(3, primaryId: 3, secondaryIds: Array.Empty<int>(), rowVersion: staleToken));
    }

    [SkippableFact]
    public async Task Rejects_a_primary_that_does_not_exist()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        var repo = NewRepository();
        var token = await RowVersionOf(3);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            repo.ReplaceAssignmentsAsync(3, primaryId: 999_999, secondaryIds: Array.Empty<int>(), rowVersion: token));
    }
}
