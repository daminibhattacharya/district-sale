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

    // ----- SetPrimary (the swap) -----

    [Fact]
    public void SetPrimary_replaces_the_primary_and_the_old_primary_drops_out()
    {
        var district = ADistrict.WithPrimary(1).Build();

        district.SetPrimary(ADistrict.Person(2));

        Assert.Equal(2, district.Primary.Id);
        Assert.DoesNotContain(district.Secondaries, s => s.Id == 1); // old primary not auto-demoted
    }

    [Fact]
    public void SetPrimary_promoting_a_secondary_removes_them_from_secondaries()
    {
        var district = ADistrict.WithPrimary(1).WithSecondaries(2).Build();

        district.SetPrimary(ADistrict.Person(2));

        Assert.Equal(2, district.Primary.Id);
        Assert.DoesNotContain(district.Secondaries, s => s.Id == 2); // no dual role (I2)
    }

    [Fact]
    public void SetPrimary_to_the_current_primary_is_a_no_op()
    {
        var district = ADistrict.WithPrimary(1).WithSecondaries(2).Build();

        district.SetPrimary(ADistrict.Person(1));

        Assert.Equal(1, district.Primary.Id);
        Assert.Single(district.Secondaries); // secondary 2 untouched
    }

    [Fact] // BR-4 / I1 — after a swap there is still exactly one primary
    public void SetPrimary_keeps_exactly_one_primary()
    {
        var district = ADistrict.WithPrimary(1).WithSecondaries(2, 3).Build();

        district.SetPrimary(ADistrict.Person(3));

        Assert.Equal(3, district.Primary.Id);
        Assert.DoesNotContain(district.Secondaries, s => s.Id == 3);
        Assert.Contains(district.Secondaries, s => s.Id == 2);
    }

    // ----- RemoveSecondary -----

    [Fact]
    public void RemoveSecondary_removes_the_salesperson()
    {
        var district = ADistrict.WithPrimary(1).WithSecondaries(2, 3).Build();

        district.RemoveSecondary(2);

        Assert.DoesNotContain(district.Secondaries, s => s.Id == 2);
        Assert.Contains(district.Secondaries, s => s.Id == 3);
    }

    [Fact] // BR-4 guard — the primary can never be removed (I1)
    public void RemoveSecondary_rejects_removing_the_primary()
    {
        var district = ADistrict.WithPrimary(1).Build();

        var ex = Assert.Throws<ConflictException>(() => district.RemoveSecondary(1));

        Assert.Equal(1, district.Primary.Id);
        Assert.Contains("replace the primary instead", ex.Message);
    }

    [Fact]
    public void RemoveSecondary_rejects_a_non_member()
    {
        var district = ADistrict.WithPrimary(1).WithSecondaries(2).Build();

        Assert.Throws<NotFoundException>(() => district.RemoveSecondary(99));
    }
}
