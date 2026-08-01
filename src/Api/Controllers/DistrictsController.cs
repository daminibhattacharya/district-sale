using Api.Contracts;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>Read + assignment endpoints for districts. Errors flow through the ProblemDetails handler.</summary>
[ApiController]
[Route("api/v1/districts")]
public sealed class DistrictsController : ControllerBase
{
    private readonly IDistrictService _districts;

    public DistrictsController(IDistrictService districts) => _districts = districts;

    /// <summary>List every district with its primary and store count.</summary>
    [HttpGet]
    public Task<IReadOnlyList<DistrictSummaryDto>> GetAll(CancellationToken ct) =>
        _districts.GetAllAsync(ct);

    /// <summary>One district in full. Unknown id → 404 (via the exception handler).</summary>
    [HttpGet("{id:int}")]
    public Task<DistrictDetailDto> GetById(int id, CancellationToken ct) =>
        _districts.GetDetailAsync(id, ct);

    /// <summary>
    /// Replace a district's whole assignment set (the full desired state). Omitting a secondary is
    /// how it is removed. A body with no primary fails validation (400); a stale concurrency token
    /// is a 409; an unknown district or salesperson is a 404. Returns the updated detail.
    /// </summary>
    [HttpPut("{id:int}/salespersons")]
    public Task<DistrictDetailDto> ReplaceAssignments(
        int id, [FromBody] PutAssignmentsRequest request, CancellationToken ct) =>
        _districts.ReplaceAssignmentsAsync(id, request, ct);
}
