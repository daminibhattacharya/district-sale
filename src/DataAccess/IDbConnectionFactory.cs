using Microsoft.Data.SqlClient;

namespace DataAccess;

/// <summary>
/// Hands out open SQL Server connections. Abstracting the connection behind an interface keeps the
/// repositories free of any knowledge of where the connection string comes from, and lets them be
/// exercised against a real database in the integration tier without static state.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>Create and open a new connection. The caller owns it and must dispose it.</summary>
    Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken ct = default);
}
