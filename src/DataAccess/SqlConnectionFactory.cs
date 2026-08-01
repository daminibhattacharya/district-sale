using Microsoft.Data.SqlClient;

namespace DataAccess;

/// <summary>
/// Creates open <see cref="SqlConnection"/>s from a single connection string. The string is supplied
/// once at construction — the composition root reads it from configuration (never hard-coded, never
/// committed), so nothing below this line depends on <c>IConfiguration</c>.
/// </summary>
public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("A SQL connection string is required.", nameof(connectionString));

        _connectionString = connectionString;
    }

    public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken ct = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
