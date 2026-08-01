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

    /// <summary>Add a salesperson as a secondary of the district.</summary>
    Task AddSecondaryAsync(int districtId, int salespersonId, CancellationToken ct = default);

    /// <summary>Make a salesperson the primary of the district (promotes from secondary if needed).</summary>
    Task SetPrimaryAsync(int districtId, int salespersonId, CancellationToken ct = default);

    /// <summary>Remove a secondary from the district.</summary>
    Task RemoveSecondaryAsync(int districtId, int salespersonId, CancellationToken ct = default);

    // ReplaceAssignmentsAsync (the PUT full-list replace with an optimistic-concurrency check)
    // arrives with C22, where its rowversion contract is designed.
}
