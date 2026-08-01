using Dapper;
using Domain;

namespace DataAccess;

/// <summary>
/// Dapper-backed persistence for the <see cref="District"/> aggregate — hand-written SQL, no ORM.
/// Each call takes its own short-lived connection from the <see cref="IDbConnectionFactory"/>.
/// </summary>
public sealed class DistrictRepository : IDistrictRepository
{
    private readonly IDbConnectionFactory _connections;

    public DistrictRepository(IDbConnectionFactory connections) => _connections = connections;

    public async Task<IReadOnlyList<DistrictSummary>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql =
            """
            SELECT  d.Id,
                    d.Name,
                    d.PrimarySalespersonId          AS PrimaryId,
                    sp.Name                         AS PrimaryName,
                    (SELECT COUNT(*) FROM dbo.Store s WHERE s.DistrictId = d.Id) AS StoreCount
            FROM    dbo.District d
            JOIN    dbo.Salesperson sp ON sp.Id = d.PrimarySalespersonId
            ORDER BY d.Name;
            """;

        await using var conn = await _connections.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<SummaryRow>(new CommandDefinition(sql, cancellationToken: ct));

        return rows.Select(r => new DistrictSummary(
            r.Id, r.Name, new Salesperson(r.PrimaryId, r.PrimaryName), r.StoreCount)).ToList();
    }

    public async Task<District?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        const string sql =
            """
            SELECT  d.Id, d.Name, d.PrimarySalespersonId AS PrimaryId, sp.Name AS PrimaryName
            FROM    dbo.District d
            JOIN    dbo.Salesperson sp ON sp.Id = d.PrimarySalespersonId
            WHERE   d.Id = @id;

            SELECT  s.Id, s.Name
            FROM    dbo.DistrictSecondarySalesperson link
            JOIN    dbo.Salesperson s ON s.Id = link.SalespersonId
            WHERE   link.DistrictId = @id
            ORDER BY s.Name;

            SELECT  st.Id, st.Name
            FROM    dbo.Store st
            WHERE   st.DistrictId = @id
            ORDER BY st.Name;
            """;

        await using var conn = await _connections.CreateOpenConnectionAsync(ct);
        using var result = await conn.QueryMultipleAsync(new CommandDefinition(sql, new { id }, cancellationToken: ct));

        var head = await result.ReadSingleOrDefaultAsync<HeadRow>();
        if (head is null)
            return null;

        var secondaries = (await result.ReadAsync<Salesperson>()).ToList();
        var stores = (await result.ReadAsync<Store>()).ToList();

        return new District(head.Id, head.Name, new Salesperson(head.PrimaryId, head.PrimaryName), secondaries, stores);
    }

    // The mutations below are each their own test-first increment and land in the following commits.
    public Task AddSecondaryAsync(int districtId, int salespersonId, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task SetPrimaryAsync(int districtId, int salespersonId, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task RemoveSecondaryAsync(int districtId, int salespersonId, CancellationToken ct = default) =>
        throw new NotImplementedException();

    // Row shapes used only to carry columns from Dapper into domain objects.
    private sealed record SummaryRow(int Id, string Name, int PrimaryId, string PrimaryName, int StoreCount);
    private sealed record HeadRow(int Id, string Name, int PrimaryId, string PrimaryName);
}
