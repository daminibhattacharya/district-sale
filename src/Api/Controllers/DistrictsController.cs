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
}
