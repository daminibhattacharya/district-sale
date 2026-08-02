using DataAccess;
using Tests.Integration.Infrastructure;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// <see cref="DistrictRepository.SetPrimaryAsync"/> against the seeded database. Two paths: promoting
/// an existing secondary (who must lose their secondary link in the same transaction, so nobody holds
/// two roles), and installing someone who was not previously involved (the old primary simply steps
/// down — it is not demoted to secondary).
/// </summary>
[Collection(SqlServerCollection.Name)]
[Trait("Category", "Integration")]
public class DistrictRepositorySetPrimaryTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sql;

    public DistrictRepositorySetPrimaryTests(SqlServerFixture sql) => _sql = sql;

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
    public async Task Promoting_a_secondary_makes_them_primary_and_drops_the_secondary_link()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        var repo = NewRepository();

        // Copenhagen (3): primary Camilla (3), secondaries Bjørn (2), Freja (6), Gustav (7).
        await repo.SetPrimaryAsync(districtId: 3, salespersonId: 2); // promote Bjørn

        var copenhagen = await repo.GetByIdAsync(3);
        Assert.Equal(2, copenhagen!.Primary.Id);                       // Bjørn is now primary
        Assert.DoesNotContain(copenhagen.Secondaries, s => s.Id == 2); // and no longer a secondary
        Assert.Equal(2, copenhagen.Secondaries.Count);                 // Freja, Gustav remain
    }

    [SkippableFact]
    public async Task Installing_an_outsider_swaps_the_primary_and_leaves_secondaries_untouched()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        var repo = NewRepository();

        // North Denmark (1): primary Anna (1), secondary Emil (5). David (4) is not involved.
        await repo.SetPrimaryAsync(districtId: 1, salespersonId: 4);

        var north = await repo.GetByIdAsync(1);
        Assert.Equal(4, north!.Primary.Id);                    // David is primary
        Assert.Contains(north.Secondaries, s => s.Id == 5);    // Emil still a secondary
        Assert.Single(north.Secondaries);                      // old primary Anna is NOT demoted in
    }
}
