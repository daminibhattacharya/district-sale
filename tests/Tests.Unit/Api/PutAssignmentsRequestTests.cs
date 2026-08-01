using System.ComponentModel.DataAnnotations;
using Api.Contracts;
using Xunit;

namespace Tests.Unit.Api;

/// <summary>
/// The PUT request contract: a district must name a primary and carry a concurrency token, and an
/// omitted secondaries array must read as "no secondaries" rather than a null-reference downstream.
/// </summary>
[Trait("Category", "Unit")]
public class PutAssignmentsRequestTests
{
    private static IReadOnlyList<ValidationResult> Validate(PutAssignmentsRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void A_well_formed_request_is_valid()
    {
        var request = new PutAssignmentsRequest(PrimaryId: 3, SecondaryIds: new[] { 1, 2 }, ConcurrencyToken: "AAAAAAAAB9E=");

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void A_request_without_a_primary_is_invalid()
    {
        var request = new PutAssignmentsRequest(PrimaryId: 0, SecondaryIds: null, ConcurrencyToken: "AAAAAAAAB9E=");

        Assert.Contains(Validate(request), r => r.MemberNames.Contains(nameof(PutAssignmentsRequest.PrimaryId)));
    }

    [Fact]
    public void A_request_without_a_concurrency_token_is_invalid()
    {
        var request = new PutAssignmentsRequest(PrimaryId: 3, SecondaryIds: null, ConcurrencyToken: null!);

        Assert.Contains(Validate(request), r => r.MemberNames.Contains(nameof(PutAssignmentsRequest.ConcurrencyToken)));
    }

    [Fact]
    public void Omitted_secondaries_read_as_an_empty_list()
    {
        var request = new PutAssignmentsRequest(PrimaryId: 3, SecondaryIds: null, ConcurrencyToken: "AAAAAAAAB9E=");

        Assert.Empty(request.Secondaries);
    }
}
