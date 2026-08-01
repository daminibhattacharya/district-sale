using Domain;

namespace DataAccess;

/// <summary>Read access to salespersons (the pool the UI assigns from).</summary>
public interface ISalespersonRepository
{
    /// <summary>Every salesperson, ordered for display.</summary>
    Task<IReadOnlyList<Salesperson>> GetAllAsync(CancellationToken ct = default);

    /// <summary>A single salesperson, or <c>null</c> if unknown.</summary>
    Task<Salesperson?> GetByIdAsync(int id, CancellationToken ct = default);
}
