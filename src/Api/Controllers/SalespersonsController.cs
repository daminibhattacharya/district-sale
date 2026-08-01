using Api.Contracts;
using DataAccess;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>The salesperson pool the UI assigns from.</summary>
[ApiController]
[Route("api/v1/salespersons")]
public sealed class SalespersonsController : ControllerBase
{
    private readonly ISalespersonRepository _salespersons;

    public SalespersonsController(ISalespersonRepository salespersons) => _salespersons = salespersons;

    /// <summary>Every salesperson, ordered for display.</summary>
    [HttpGet]
    public async Task<IReadOnlyList<SalespersonDto>> GetAll(CancellationToken ct)
    {
        var people = await _salespersons.GetAllAsync(ct);
        return people.Select(s => new SalespersonDto(s.Id, s.Name)).ToList();
    }
}
