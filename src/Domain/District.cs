namespace Domain;

/// <summary>
/// A district and the salespersons/stores associated with it — the aggregate that holds the
/// business-rule invariants in plain C# (so they are unit-testable without a database). The SQL
/// schema enforces the same rules as a backstop (defense in depth).
///
/// Invariants held at all times:
///   I1  exactly one primary salesperson (<see cref="Primary"/> is never null) — BR-4;
///   I2  nobody is both the primary and a secondary of this district — BR-7;
///   I3  a salesperson appears at most once as a secondary.
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

    /// <summary>Add a salesperson as a secondary of this district.</summary>
    public void AddSecondary(Salesperson salesperson)
    {
        ArgumentNullException.ThrowIfNull(salesperson);

        if (salesperson.Id == Primary.Id) // BR-7 — no dual role (I2)
            throw new ConflictException(
                $"Salesperson {salesperson.Id} is already the primary of district {Id}.");

        if (_secondaries.Any(x => x.Id == salesperson.Id)) // no duplicate (I3)
            throw new ConflictException(
                $"Salesperson {salesperson.Id} is already a secondary of district {Id}.");

        _secondaries.Add(salesperson);
    }

    /// <summary>
    /// Reassign the primary salesperson (the "swap"). Setting the current primary again is an
    /// idempotent no-op. If the new primary is currently a secondary they are removed from the
    /// secondaries so nobody holds two roles (I2). The displaced old primary simply stops being
    /// primary — it is not auto-demoted to secondary. A district can never be left without a
    /// primary, so there is no "remove primary" (I1 / BR-4).
    /// </summary>
    public void SetPrimary(Salesperson salesperson)
    {
        ArgumentNullException.ThrowIfNull(salesperson);

        if (salesperson.Id == Primary.Id) // idempotent
            return;

        _secondaries.RemoveAll(x => x.Id == salesperson.Id); // promote from secondary (I2)
        Primary = salesperson;
    }
}
