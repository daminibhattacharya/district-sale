using DataAccess;
using Tests.Integration.Infrastructure;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// <see cref="DistrictRepository.RemoveSecondaryAsync"/> against the seeded database: it deletes the
/// secondary link, and removing a link that is not there is a harmless no-op (the domain aggregate
/// owns the "not a member" and "can't remove the primary" rules; the data layer just deletes).
/// </summary>
[Collection(SqlServerCollection.Name)]
[Trait("Category", "Integration")]
public class DistrictRepositoryRemoveSecondaryTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sql;

    public DistrictRepositoryRemoveSecondaryTests(SqlServerFixture sql) => _sql = sql;

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

    [SkippableFact]
    public async Task Removes_an_existing_secondary()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        var repo = NewRepository();

        // Copenhagen (3) has Bjørn (2), Freja (6), Gustav (7).
        await repo.RemoveSecondaryAsync(districtId: 3, salespersonId: 6); // remove Freja

        var copenhagen = await repo.GetByIdAsync(3);
        Assert.DoesNotContain(copenhagen!.Secondaries, s => s.Id == 6);
        Assert.Equal(2, copenhagen.Secondaries.Count);
    }

    [SkippableFact]
    public async Task Removing_a_link_that_is_not_there_changes_nothing()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        var repo = NewRepository();

        // Salesperson 999 is not a secondary of Copenhagen (3).
        await repo.RemoveSecondaryAsync(districtId: 3, salespersonId: 999);

        var copenhagen = await repo.GetByIdAsync(3);
        Assert.Equal(3, copenhagen!.Secondaries.Count); // unchanged
    }
}
