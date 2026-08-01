using DataAccess;
using Tests.Integration.Infrastructure;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// Read side of the district repository against the seeded database. The seed (010_seed.sql) is the
/// fixture: known districts with known store/secondary counts, so these assert exact hydration —
/// the list summaries and the full aggregate — rather than just "some rows came back".
/// </summary>
[Collection(SqlServerCollection.Name)]
[Trait("Category", "Integration")]
public class DistrictRepositoryReadTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sql;

    public DistrictRepositoryReadTests(SqlServerFixture sql) => _sql = sql;

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
    public async Task GetAll_returns_every_district_with_its_primary_and_store_count()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        var repo = NewRepository();

        var all = await repo.GetAllAsync();

        Assert.Equal(5, all.Count);

        var north = Assert.Single(all, d => d.Name == "North Denmark");
        Assert.Equal("Anna Bruun", north.Primary.Name); // primary joined in
        Assert.Equal(4, north.StoreCount);

        var zealand = Assert.Single(all, d => d.Name == "Zealand");
        Assert.Equal(0, zealand.StoreCount); // the empty-store district
    }

    [SkippableFact]
    public async Task GetById_hydrates_the_full_aggregate()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        var repo = NewRepository();

        var copenhagen = await repo.GetByIdAsync(3);

        Assert.NotNull(copenhagen);
        Assert.Equal("Copenhagen", copenhagen!.Name);
        Assert.Equal("Camilla Holm", copenhagen.Primary.Name);
        Assert.Equal(3, copenhagen.Secondaries.Count); // Bjørn, Freja, Gustav
        Assert.Equal(5, copenhagen.Stores.Count);
    }

    [SkippableFact]
    public async Task GetById_hydrates_a_district_with_no_secondaries_or_stores()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        var repo = NewRepository();

        var zealand = await repo.GetByIdAsync(5); // Zealand: 0 secondaries, 0 stores in seed

        Assert.NotNull(zealand);
        Assert.Equal("Emil Lund", zealand!.Primary.Name);
        Assert.Empty(zealand.Secondaries);
        Assert.Empty(zealand.Stores);
    }

    [SkippableFact]
    public async Task GetById_returns_null_for_an_unknown_district()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        var repo = NewRepository();

        Assert.Null(await repo.GetByIdAsync(999_999));
    }
}
