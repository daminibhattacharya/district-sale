using Dapper;
using Domain;
using Microsoft.Data.SqlClient;

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

    public async Task AddSecondaryAsync(int districtId, int salespersonId, CancellationToken ct = default)
    {
        const string sql =
            "INSERT INTO dbo.DistrictSecondarySalesperson (DistrictId, SalespersonId) " +
            "VALUES (@districtId, @salespersonId);";

        await using var conn = await _connections.CreateOpenConnectionAsync(ct);
        try
        {
            await conn.ExecuteAsync(new CommandDefinition(sql, new { districtId, salespersonId }, cancellationToken: ct));
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627) // unique/PK violation — already a secondary
        {
            throw new ConflictException(
                $"Salesperson {salespersonId} is already a secondary of district {districtId}.");
        }
    }

    public async Task SetPrimaryAsync(int districtId, int salespersonId, CancellationToken ct = default)
    {
        await using var conn = await _connections.CreateOpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        // Promoting a current secondary must not leave them holding both roles (I2 / BR-7), so drop
        // the secondary link (a no-op when they were not a secondary) and set the primary — atomically.
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM dbo.DistrictSecondarySalesperson WHERE DistrictId = @districtId AND SalespersonId = @salespersonId;",
            new { districtId, salespersonId }, tx, cancellationToken: ct));

        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE dbo.District SET PrimarySalespersonId = @salespersonId WHERE Id = @districtId;",
            new { districtId, salespersonId }, tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
    }

    public async Task RemoveSecondaryAsync(int districtId, int salespersonId, CancellationToken ct = default)
    {
        const string sql =
            "DELETE FROM dbo.DistrictSecondarySalesperson " +
            "WHERE DistrictId = @districtId AND SalespersonId = @salespersonId;";

        await using var conn = await _connections.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(sql, new { districtId, salespersonId }, cancellationToken: ct));
    }

    public async Task<byte[]> ReplaceAssignmentsAsync(
        int districtId,
        int primaryId,
        IReadOnlyCollection<int> secondaryIds,
        byte[] rowVersion,
        CancellationToken ct = default)
    {
        await using var conn = await _connections.CreateOpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            // Optimistic-concurrency gate: the set-primary write only lands if the token still
            // matches. Any UPDATE bumps the district's rowversion, so this is also what advances it.
            var affected = await conn.ExecuteAsync(new CommandDefinition(
                "UPDATE dbo.District SET PrimarySalespersonId = @primaryId WHERE Id = @districtId AND RowVersion = @rowVersion;",
                new { districtId, primaryId, rowVersion }, tx, cancellationToken: ct));

            if (affected == 0)
                throw new ConcurrencyException(
                    $"District {districtId} was changed by someone else since it was read; reload and retry.");

            // Replace the secondary set wholesale: clear it, then insert the requested list.
            await conn.ExecuteAsync(new CommandDefinition(
                "DELETE FROM dbo.DistrictSecondarySalesperson WHERE DistrictId = @districtId;",
                new { districtId }, tx, cancellationToken: ct));

            if (secondaryIds.Count > 0)
                await conn.ExecuteAsync(new CommandDefinition(
                    "INSERT INTO dbo.DistrictSecondarySalesperson (DistrictId, SalespersonId) VALUES (@districtId, @salespersonId);",
                    secondaryIds.Select(id => new { districtId, salespersonId = id }), tx, cancellationToken: ct));

            var newRowVersion = await conn.ExecuteScalarAsync<byte[]>(new CommandDefinition(
                "SELECT RowVersion FROM dbo.District WHERE Id = @districtId;",
                new { districtId }, tx, cancellationToken: ct));

            await tx.CommitAsync(ct);
            return newRowVersion!;
        }
        catch (SqlException ex) when (ex.Number == 547) // FK — a referenced salesperson does not exist
        {
            throw new NotFoundException("One or more salespersons in the assignment do not exist.");
        }
    }

    // Row shapes used only to carry columns from Dapper into domain objects.
    private sealed record SummaryRow(int Id, string Name, int PrimaryId, string PrimaryName, int StoreCount);
    private sealed record HeadRow(int Id, string Name, int PrimaryId, string PrimaryName);
}
