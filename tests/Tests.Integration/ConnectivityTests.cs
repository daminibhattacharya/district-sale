using Dapper;
using Tests.Integration.Infrastructure;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// Proves the integration harness can reach a real SQL Server from a test — the prerequisite for
/// every business-rule test that follows (those assert the database itself rejects a violation).
/// </summary>
[Collection(SqlServerCollection.Name)]
[Trait("Category", "Integration")]
public class ConnectivityTests
{
    private readonly SqlServerFixture _sql;

    public ConnectivityTests(SqlServerFixture sql) => _sql = sql;

    [SkippableFact]
    public async Task Fixture_can_connect_to_the_test_database()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");

        await using var connection = await _sql.OpenConnectionAsync();
        var result = await connection.QuerySingleAsync<int>("SELECT 1;");

        Assert.Equal(1, result);
    }
}
