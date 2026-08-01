using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;

namespace DataAccess;

/// <summary>
/// Development-only bootstrapper: when the database has no districts yet, applies the versioned
/// schema then seed scripts so a freshly-pointed (empty) database becomes runnable — "clone and run"
/// with no manual DB setup, and the harness the E2E stands up against. Idempotent: once seeded it is a
/// no-op, so restarts never wipe data. Wired up only in Development (see Program.cs).
/// </summary>
public sealed partial class DbInitializer
{
    private readonly IDbConnectionFactory _factory;

    public DbInitializer(IDbConnectionFactory factory) => _factory = factory;

    public async Task EnsureSeededAsync(CancellationToken ct = default)
    {
        await using var connection = await _factory.CreateOpenConnectionAsync(ct);

        var districtCount = await connection.ExecuteScalarAsync<int>(
            """
            IF OBJECT_ID('dbo.District', 'U') IS NULL SELECT 0;
            ELSE SELECT COUNT(*) FROM dbo.District;
            """);
        if (districtCount > 0)
            return; // already initialised — leave the data alone.

        await RunScriptAsync(connection, "001_schema.sql", ct);
        await RunScriptAsync(connection, "010_seed.sql", ct);
    }

    private static async Task RunScriptAsync(SqlConnection connection, string fileName, CancellationToken ct)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Sql", fileName);
        var script = await File.ReadAllTextAsync(path, ct);

        // Each 'GO'-separated batch runs as its own command.
        foreach (var batch in GoSeparator().Split(script))
        {
            if (!string.IsNullOrWhiteSpace(batch))
                await connection.ExecuteAsync(batch);
        }
    }

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex GoSeparator();
}
