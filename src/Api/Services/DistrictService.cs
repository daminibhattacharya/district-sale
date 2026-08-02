using Api.Contracts;
using DataAccess;
using Domain;

namespace Api.Services;

public interface IDistrictService
{
    Task<IReadOnlyList<DistrictSummaryDto>> GetAllAsync(CancellationToken ct = default);
    Task<DistrictDetailDto> GetDetailAsync(int districtId, CancellationToken ct = default);
    Task<DistrictDetailDto> ReplaceAssignmentsAsync(int districtId, PutAssignmentsRequest request, CancellationToken ct = default);
}

/// <summary>
/// Orchestration only — it does not invent business rules. It loads the <see cref="District"/>
/// aggregate, checks that referenced resources exist (→ 404), delegates rule enforcement to the
/// domain object (→ 409 via typed exceptions), persists the change under optimistic concurrency,
/// and maps between the domain and the wire DTOs (including the rowversion ⇄ base64 token).
/// </summary>
public sealed class DistrictService : IDistrictService
{
    private readonly IDistrictRepository _districts;
    private readonly ISalespersonRepository _salespersons;

    public DistrictService(IDistrictRepository districts, ISalespersonRepository salespersons)
    {
        _districts = districts;
        _salespersons = salespersons;
    }

    public async Task<IReadOnlyList<DistrictSummaryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var districts = await _districts.GetAllAsync(ct);
        return districts
            .Select(d => new DistrictSummaryDto(d.Id, d.Name, ToDto(d.Primary), d.StoreCount))
            .ToList();
    }

    public async Task<DistrictDetailDto> GetDetailAsync(int districtId, CancellationToken ct = default)
    {
        var district = await _districts.GetByIdAsync(districtId, ct)
            ?? throw new NotFoundException($"District {districtId} was not found.");
        var rowVersion = await _districts.GetRowVersionAsync(districtId, ct)
            ?? throw new NotFoundException($"District {districtId} was not found.");

        return ToDetail(district, rowVersion);
    }

    public async Task<DistrictDetailDto> ReplaceAssignmentsAsync(
        int districtId, PutAssignmentsRequest request, CancellationToken ct = default)
    {
        var existing = await _districts.GetByIdAsync(districtId, ct)
            ?? throw new NotFoundException($"District {districtId} was not found.");

        var primary = await GetSalespersonAsync(request.PrimaryId, ct);

        // Validate the requested end-state through the aggregate, so the same invariants apply as
        // anywhere else (no dual role — BR-7; no duplicate secondary). Name and stores are unchanged.
        var target = new District(existing.Id, existing.Name, primary, secondaries: null, stores: existing.Stores);
        foreach (var secondaryId in request.Secondaries)
            target.AddSecondary(await GetSalespersonAsync(secondaryId, ct));

        var rowVersion = ParseToken(request.ConcurrencyToken);
        var newRowVersion = await _districts.ReplaceAssignmentsAsync(
            districtId, request.PrimaryId, request.Secondaries, rowVersion, ct);

        return ToDetail(target, newRowVersion);
    }

    private async Task<Salesperson> GetSalespersonAsync(int id, CancellationToken ct) =>
        await _salespersons.GetByIdAsync(id, ct)
        ?? throw new NotFoundException($"Salesperson {id} was not found.");

    private static byte[] ParseToken(string token)
    {
        try
        {
            return Convert.FromBase64String(token);
        }
        catch (FormatException)
        {
            throw new ConcurrencyException("The concurrency token is not valid; reload the district and retry.");
        }
    }

    private static SalespersonDto ToDto(Salesperson s) => new(s.Id, s.Name);

    private static DistrictDetailDto ToDetail(District d, byte[] rowVersion) => new(
        d.Id,
        d.Name,
        ToDto(d.Primary),
        d.Secondaries.Select(ToDto).ToList(),
        d.Stores.Select(s => new StoreDto(s.Id, s.Name)).ToList(),
        Convert.ToBase64String(rowVersion));
}
