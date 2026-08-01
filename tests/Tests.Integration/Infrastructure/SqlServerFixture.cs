using Microsoft.Data.SqlClient;
using Xunit;

namespace Tests.Integration.Infrastructure;

/// <summary>
/// Shared SQL Server harness for the integration tier. The connection string comes from the
/// <c>DISTRICT_SQL_TEST</c> environment variable (a throwaway test database) — never from Git.
/// When it is not set, integration tests <see cref="Skip"/> rather than fail, so a contributor
/// without database access (or CI before the secret is configured) still gets a green unit run.
///
/// Schema application and between-test reset (Respawn) are layered on as the schema lands.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    public const string ConnectionStringEnvVar = "DISTRICT_SQL_TEST";

    public string? ConnectionString { get; } =
        Environment.GetEnvironmentVariable(ConnectionStringEnvVar);

    public bool IsAvailable => !string.IsNullOrWhiteSpace(ConnectionString);

    public async Task<SqlConnection> OpenConnectionAsync(CancellationToken ct = default)
    {
        var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(ct);
        return connection;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;
}

/// <summary>
/// Groups every integration test into one xUnit collection so they share the single
/// <see cref="SqlServerFixture"/> and run serially (no parallel collisions on the shared DB).
/// </summary>
[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "sql-server";
}
