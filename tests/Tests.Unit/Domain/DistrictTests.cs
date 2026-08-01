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
}
