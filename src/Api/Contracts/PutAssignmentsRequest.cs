using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

/// <summary>
/// Request body for <c>PUT /api/v1/districts/{id}/salespersons</c> — the full desired assignment
/// state. The primary is required (a district always has exactly one, BR-4; a body with no primary
/// is rejected), and the secondaries are the complete list: omitting one <em>is</em> its removal.
/// <see cref="ConcurrencyToken"/> is the rowversion last read for this district (optimistic locking).
/// </summary>
public sealed record PutAssignmentsRequest(
    [property: Range(1, int.MaxValue, ErrorMessage = "primaryId must be a positive salesperson id.")]
    int PrimaryId,

    IReadOnlyList<int>? SecondaryIds,

    [property: Required(ErrorMessage = "concurrencyToken is required.")]
    string ConcurrencyToken)
{
    /// <summary>Secondaries as a non-null list — an omitted array means "no secondaries".</summary>
    public IReadOnlyList<int> Secondaries => SecondaryIds ?? Array.Empty<int>();
}
