namespace Api.Contracts;

/// <summary>
/// Response contracts. The API never returns domain entities directly — these are immutable,
/// serialization-friendly projections that decouple the wire shape from the domain model.
/// </summary>
public sealed record SalespersonDto(int Id, string Name);

public sealed record StoreDto(int Id, string Name);

/// <summary>List-view row: a district, who is in charge, and how many stores it has.</summary>
public sealed record DistrictSummaryDto(int Id, string Name, SalespersonDto Primary, int StoreCount);

/// <summary>
/// Detail view: the full picture for one district. <see cref="ConcurrencyToken"/> is the district's
/// rowversion (base64) — the client echoes it back on a PUT so a stale write is rejected (409).
/// </summary>
public sealed record DistrictDetailDto(
    int Id,
    string Name,
    SalespersonDto Primary,
    IReadOnlyList<SalespersonDto> Secondaries,
    IReadOnlyList<StoreDto> Stores,
    string ConcurrencyToken);
