using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;
using Respawn;
using Xunit;

namespace Tests.Integration.Infrastructure;

/// <summary>
/// Shared SQL Server harness for the integration tier. The connection string comes from the
/// <c>DISTRICT_SQL_TEST</c> environment variable (a throwaway test database) — never from Git.
/// When it is not set, integration tests <see cref="Skip"/> rather than fail, so a contributor
/// without database access (or CI before the secret is configured) still gets a green unit run.
///
/// On startup it applies the versioned <c>Sql/*.sql</c> scripts (the schema of record), then
/// captures a Respawn checkpoint of the empty database. Tests call <see cref="ResetAsync"/> to
/// return to that clean state, keeping each test isolated and repeatable.
/// </summary>
public sealed partial class SqlServerFixture : IAsyncLifetime
{
    public const string ConnectionStringEnvVar = "DISTRICT_SQL_TEST";

    public string? ConnectionString { get; } =
        Environment.GetEnvironmentVariable(ConnectionStringEnvVar);

    public bool IsAvailable => !string.IsNullOrWhiteSpace(ConnectionString);

    private Respawner? _respawner;

    public async Task<SqlConnection> OpenConnectionAsync(CancellationToken ct = default)
    {
        var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(ct);
        return connection;
    }

    public async Task InitializeAsync()
    {
        if (!IsAvailable)
            return; // no DB configured — every integration test will Skip.

        await ApplySchemaAsync();

        await using var connection = await OpenConnectionAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            SchemasToInclude = ["dbo"],
            WithReseed = true // reset IDENTITY seeds so ids are stable across tests
        });
    }

    /// <summary>Delete all data and reset identities back to the post-schema clean state.</summary>
    public async Task ResetAsync()
    {
        if (_respawner is null)
            return;

        await using var connection = await OpenConnectionAsync();
        await _respawner.ResetAsync(connection);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task ApplySchemaAsync()
    {
        var scriptsDir = Path.Combine(AppContext.BaseDirectory, "Sql");
        var scripts = Directory.GetFiles(scriptsDir, "*.sql").OrderBy(f => f, StringComparer.Ordinal);

        await using var connection = await OpenConnectionAsync();
        foreach (var path in scripts)
        {
            var script = await File.ReadAllTextAsync(path);
            foreach (var batch in GoSeparator().Split(script))
            {
                if (!string.IsNullOrWhiteSpace(batch))
                    await connection.ExecuteAsync(batch);
            }
        }
    }

    // Split a script into batches on the T-SQL 'GO' separator (its own line).
    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex GoSeparator();
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
