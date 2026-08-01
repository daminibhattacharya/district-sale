using Domain;

namespace DataAccess;

/// <summary>
/// Lightweight read model for the district list: enough to render a row (name, who is in charge,
/// how many stores) without hydrating every secondary and store of every district. The full
/// aggregate is loaded only when one district is opened (<see cref="IDistrictRepository.GetByIdAsync"/>).
/// </summary>
public sealed record DistrictSummary(int Id, string Name, Salesperson Primary, int StoreCount);
