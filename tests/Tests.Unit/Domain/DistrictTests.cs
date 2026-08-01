using Domain;
using Xunit;

namespace Tests.Unit.Domain;

/// <summary>
/// Pure domain unit tests — plain objects, no mocks, no database. These drive the business
/// rules that live in <see cref="District"/>, the invariant-holding aggregate.
/// </summary>
[Trait("Category", "Unit")]
public class DistrictTests
{
    // ----- Construction / BR-4 invariant: a district always has exactly one primary -----

    [Fact]
    public void Constructing_a_district_without_a_primary_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(
            () => new District(1, "North Denmark", primary: null!));
    }

    // ----- BR-1: every district has a name -----

    [Fact]
    public void Constructing_a_district_without_a_name_is_rejected()
    {
        Assert.Throws<ArgumentException>(
            () => new District(1, "  ", ADistrict.Person(1)));
    }

    // ----- AddSecondary -----

    [Fact]
    public void AddSecondary_adds_the_salesperson_to_secondaries()
    {
        var district = ADistrict.WithPrimary(1).Build();

        district.AddSecondary(ADistrict.Person(2));

        Assert.Contains(district.Secondaries, s => s.Id == 2);
    }

    [Fact]
    public void AddSecondary_rejects_a_duplicate_secondary()
    {
        var district = ADistrict.WithPrimary(1).WithSecondaries(2).Build();

        var ex = Assert.Throws<ConflictException>(() => district.AddSecondary(ADistrict.Person(2)));

        Assert.Single(district.Secondaries);
        Assert.Contains("already a secondary", ex.Message);
    }

    [Fact] // BR-7: a salesperson can't be both primary and secondary in the same district
    public void AddSecondary_rejects_the_current_primary()
    {
        var district = ADistrict.WithPrimary(1).Build();

        var ex = Assert.Throws<ConflictException>(() => district.AddSecondary(ADistrict.Person(1)));

        Assert.Empty(district.Secondaries);
        Assert.Contains("already the primary", ex.Message);
    }
}
