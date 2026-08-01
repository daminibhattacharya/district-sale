using Api.Contracts;
using Xunit;

namespace Tests.Unit.Api;

/// <summary>
/// The PUT request contract. The DataAnnotations on the record's parameters (required primary/token)
/// are enforced by MVC model binding at the API boundary — see the 400 case in the endpoint contract
/// tests. Here we pin the piece with logic: an omitted secondaries array reads as "no secondaries".
/// </summary>
[Trait("Category", "Unit")]
public class PutAssignmentsRequestTests
{
    [Fact]
    public void Omitted_secondaries_read_as_an_empty_list()
    {
        var request = new PutAssignmentsRequest(PrimaryId: 3, SecondaryIds: null, ConcurrencyToken: "AAAAAAAAB9E=");

        Assert.Empty(request.Secondaries);
    }

    [Fact]
    public void Provided_secondaries_are_exposed_as_is()
    {
        var request = new PutAssignmentsRequest(PrimaryId: 3, SecondaryIds: new[] { 2, 8 }, ConcurrencyToken: "AAAAAAAAB9E=");

        Assert.Equal(new[] { 2, 8 }, request.Secondaries);
    }
}
