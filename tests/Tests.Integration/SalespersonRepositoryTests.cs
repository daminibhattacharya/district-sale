using DataAccess;
using Tests.Integration.Infrastructure;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// <see cref="SalespersonRepository"/> against the seeded database — the pool the UI assigns from.
/// The seed has 8 salespersons with known names, so these assert exact reads.
/// </summary>
[Collection(SqlServerCollection.Name)]
[Trait("Category", "Integration")]
public class SalespersonRepositoryTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sql;

    public SalespersonRepositoryTests(SqlServerFixture sql) => _sql = sql;

    public async Task InitializeAsync()
    {
        if (!_sql.IsAvailable)
            return;
        await _sql.ResetAsync();
        await _sql.ApplySeedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private SalespersonRepository NewRepository() =>
        new(new SqlConnectionFactory(_sql.ConnectionString!));

    [SkippableFact]
    public async Task GetAll_returns_every_salesperson_ordered_by_name()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        var repo = NewRepository();

        var all = await repo.GetAllAsync();

        Assert.Equal(8, all.Count);
        Assert.Equal("Anna Bruun", all[0].Name); // ordered by name — Anna sorts first
        Assert.Equal(all.Select(s => s.Name).OrderBy(n => n), all.Select(s => s.Name));
    }

    [SkippableFact]
    public async Task GetById_returns_the_salesperson()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        var repo = NewRepository();

        var person = await repo.GetByIdAsync(1);

        Assert.NotNull(person);
        Assert.Equal("Anna Bruun", person!.Name);
    }

    [SkippableFact]
    public async Task GetById_returns_null_for_an_unknown_salesperson()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        var repo = NewRepository();

        Assert.Null(await repo.GetByIdAsync(999_999));
    }
}
