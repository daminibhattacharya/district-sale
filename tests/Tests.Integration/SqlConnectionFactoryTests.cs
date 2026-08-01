using System.Data;
using DataAccess;
using Tests.Integration.Infrastructure;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// The connection factory against a real database: given the test connection string, it hands back a
/// connection that is already open and ready to query — the foundation the repositories build on.
/// </summary>
[Collection(SqlServerCollection.Name)]
[Trait("Category", "Integration")]
public class SqlConnectionFactoryTests
{
    private readonly SqlServerFixture _sql;

    public SqlConnectionFactoryTests(SqlServerFixture sql) => _sql = sql;

    [SkippableFact]
    public async Task Creates_an_open_connection_from_the_configured_string()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        var factory = new SqlConnectionFactory(_sql.ConnectionString!);

        await using var connection = await factory.CreateOpenConnectionAsync();

        Assert.Equal(ConnectionState.Open, connection.State);
    }
}
