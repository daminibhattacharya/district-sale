namespace Domain;

/// <summary>
/// A district and the salespersons/stores associated with it — the aggregate that holds the
/// business-rule invariants in plain C# (so they are unit-testable without a database). The SQL
/// schema enforces the same rules as a backstop (defense in depth).
///
/// Invariants held at all times:
///   I1  exactly one primary salesperson (<see cref="Primary"/> is never null) — BR-4.
/// </summary>
public class District
{
    private readonly List<Salesperson> _secondaries;
    private readonly List<Store> _stores;

    public int Id { get; }
    public string Name { get; }

    /// <summary>The single primary salesperson. Never null (I1 / BR-4).</summary>
    public Salesperson Primary { get; private set; }

    public IReadOnlyList<Salesperson> Secondaries => _secondaries;
    public IReadOnlyList<Store> Stores => _stores;

    public District(
        int id,
        string name,
        Salesperson primary,
        IEnumerable<Salesperson>? secondaries = null,
        IEnumerable<Store>? stores = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("District name is required.", nameof(name));
        ArgumentNullException.ThrowIfNull(primary);

        Id = id;
        Name = name;
        Primary = primary;
        _secondaries = secondaries?.ToList() ?? new List<Salesperson>();
        _stores = stores?.ToList() ?? new List<Store>();
    }
}
