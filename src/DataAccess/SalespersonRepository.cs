using Dapper;
using Domain;

namespace DataAccess;

/// <summary>Dapper-backed read access to salespersons — hand-written SQL, no ORM.</summary>
public sealed class SalespersonRepository : ISalespersonRepository
{
    private readonly IDbConnectionFactory _connections;

    public SalespersonRepository(IDbConnectionFactory connections) => _connections = connections;

    public async Task<IReadOnlyList<Salesperson>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT Id, Name FROM dbo.Salesperson ORDER BY Name;";

        await using var conn = await _connections.CreateOpenConnectionAsync(ct);
        var people = await conn.QueryAsync<Salesperson>(new CommandDefinition(sql, cancellationToken: ct));
        return people.ToList();
    }

    public async Task<Salesperson?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        const string sql = "SELECT Id, Name FROM dbo.Salesperson WHERE Id = @id;";

        await using var conn = await _connections.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Salesperson>(new CommandDefinition(sql, new { id }, cancellationToken: ct));
    }
}
