using DataAccess;
using Domain;
using Tests.Integration.Infrastructure;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// <see cref="DistrictRepository.AddSecondaryAsync"/> against the seeded database: it inserts the
/// secondary link, and the composite primary key (BR-7's duplicate guard) surfaces as a
/// <see cref="ConflictException"/> rather than a raw <c>SqlException</c> leaking out of the data layer.
/// </summary>
[Collection(SqlServerCollection.Name)]
[Trait("Category", "Integration")]
public class DistrictRepositoryAddSecondaryTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sql;

    public DistrictRepositoryAddSecondaryTests(SqlServerFixture sql) => _sql = sql;

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
    public async Task Adds_a_new_secondary_to_the_district()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        var repo = NewRepository();

        await repo.AddSecondaryAsync(districtId: 1, salespersonId: 4); // David, not yet in North Denmark

        var north = await repo.GetByIdAsync(1);
        Assert.Contains(north!.Secondaries, s => s.Id == 4);
    }

    [SkippableFact]
    public async Task Adding_the_same_secondary_twice_is_a_conflict()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        var repo = NewRepository();

        // Bjørn (2) is already a secondary of Copenhagen (3) in the seed.
        await Assert.ThrowsAsync<ConflictException>(() => repo.AddSecondaryAsync(districtId: 3, salespersonId: 2));
    }
}
