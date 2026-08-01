using Domain;

namespace DataAccess;

/// <summary>
/// Persistence for the <see cref="District"/> aggregate, written in hand-rolled SQL (Dapper, no ORM).
/// Reads hydrate domain objects; mutations mirror the aggregate's own operations so the database and
/// the in-memory invariants stay in step. Implementations live alongside this interface and are
/// exercised against a real database in the integration tier.
/// </summary>
public interface IDistrictRepository
{
    /// <summary>All districts as lightweight summaries (incl. store count) for the list view.</summary>
    Task<IReadOnlyList<DistrictSummary>> GetAllAsync(CancellationToken ct = default);

    /// <summary>The full aggregate — primary, secondaries and stores — or <c>null</c> if unknown.</summary>
    Task<District?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// The district's current rowversion (its optimistic-concurrency token), or <c>null</c> if the
    /// district is unknown. Surfaced so a detail response can hand the client a token to PUT back.
    /// </summary>
    Task<byte[]?> GetRowVersionAsync(int districtId, CancellationToken ct = default);

    /// <summary>Add a salesperson as a secondary of the district.</summary>
    Task AddSecondaryAsync(int districtId, int salespersonId, CancellationToken ct = default);

    /// <summary>Make a salesperson the primary of the district (promotes from secondary if needed).</summary>
    Task SetPrimaryAsync(int districtId, int salespersonId, CancellationToken ct = default);

    /// <summary>Remove a secondary from the district.</summary>
    Task RemoveSecondaryAsync(int districtId, int salespersonId, CancellationToken ct = default);

    /// <summary>
    /// Replace a district's whole assignment set in one transaction — set the primary and make the
    /// secondaries exactly <paramref name="secondaryIds"/> (a shorter list is how a removal is
    /// expressed). Guarded by optimistic concurrency: <paramref name="rowVersion"/> must still match
    /// or a <see cref="Domain.ConcurrencyException"/> is thrown. Returns the district's new rowversion.
    /// </summary>
    Task<byte[]> ReplaceAssignmentsAsync(
        int districtId,
        int primaryId,
        IReadOnlyCollection<int> secondaryIds,
        byte[] rowVersion,
        CancellationToken ct = default);
}
