using DataAccess;
using Tests.Integration.Infrastructure;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// <see cref="DistrictRepository.GetRowVersionAsync"/> — the token a detail response hands back to
/// the client. Returns a non-null token for a real district and null for an unknown one.
/// </summary>
[Collection(SqlServerCollection.Name)]
[Trait("Category", "Integration")]
public class DistrictRepositoryRowVersionTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sql;

    public DistrictRepositoryRowVersionTests(SqlServerFixture sql) => _sql = sql;

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
    public async Task Returns_a_token_for_a_known_district()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        var repo = NewRepository();

        var token = await repo.GetRowVersionAsync(1);

        Assert.NotNull(token);
        Assert.NotEmpty(token!);
    }

    [SkippableFact]
    public async Task Returns_null_for_an_unknown_district()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        var repo = NewRepository();

        Assert.Null(await repo.GetRowVersionAsync(999_999));
    }
}
