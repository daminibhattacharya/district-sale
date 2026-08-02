using Domain;

namespace Tests.Unit.Domain;

/// <summary>
/// Test-data builder for <see cref="District"/>. Keeps the arrange step in the tests short and
/// intention-revealing: <c>ADistrict.WithPrimary(1).WithSecondaries(2, 3).Build()</c>.
/// </summary>
internal sealed class ADistrict
{
    private int _id = 1;
    private string _name = "North Denmark";
    private Salesperson _primary = Person(1);
    private readonly List<Salesperson> _secondaries = new();

    public static Salesperson Person(int id) => new(id, $"Person {id}");

    public static ADistrict WithPrimary(int primaryId) =>
        new() { _primary = Person(primaryId) };

    public ADistrict Id(int id) { _id = id; return this; }
    public ADistrict Named(string name) { _name = name; return this; }

    public ADistrict WithSecondaries(params int[] ids)
    {
        _secondaries.AddRange(ids.Select(Person));
        return this;
    }

    public District Build() => new(_id, _name, _primary, _secondaries);
}
