using Dapper;
using Tests.Integration.Infrastructure;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// The seed script (<c>010_seed.sql</c>) is the shared fixture the later data-layer and API tests
/// query, so its edge cases are pinned here: every §6.2 shape must exist after seeding, and the
/// script must be re-runnable (applying it twice leaves the same rows). Each test seeds a freshly
/// reset database, then asserts one edge case.
/// </summary>
[Collection(SqlServerCollection.Name)]
[Trait("Category", "Integration")]
public class SeedDataTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sql;

    public SeedDataTests(SqlServerFixture sql) => _sql = sql;

    public async Task InitializeAsync()
    {
        if (!_sql.IsAvailable)
            return;
        await _sql.ResetAsync();
        await _sql.ApplySeedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [SkippableFact]
    public async Task Seeds_the_target_volume_of_rows()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        await using var conn = await _sql.OpenConnectionAsync();

        Assert.Equal(5, await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.District;"));
        Assert.Equal(8, await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Salesperson;"));

        var stores = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Store;");
        Assert.InRange(stores, 15, 25);
    }

    [SkippableFact]
    public async Task A_salesperson_is_primary_of_more_than_one_district()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        await using var conn = await _sql.OpenConnectionAsync();

        var maxDistrictsHeaded = await conn.ExecuteScalarAsync<int>(
            "SELECT MAX(cnt) FROM (SELECT COUNT(*) AS cnt FROM dbo.District GROUP BY PrimarySalespersonId) g;");
        Assert.True(maxDistrictsHeaded >= 2, "expected a salesperson who is primary of ≥2 districts (BR-6)");
    }

    [SkippableFact]
    public async Task Has_a_district_with_no_secondaries_and_one_with_three()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        await using var conn = await _sql.OpenConnectionAsync();

        // One secondary-count per district.
        var secondaryCounts = (await conn.QueryAsync<int>(
            @"SELECT COUNT(s.SalespersonId)
              FROM dbo.District d
              LEFT JOIN dbo.DistrictSecondarySalesperson s ON s.DistrictId = d.Id
              GROUP BY d.Id;")).ToList();

        Assert.Contains(0, secondaryCounts);
        Assert.Contains(3, secondaryCounts);
    }

    [SkippableFact]
    public async Task A_salesperson_is_secondary_in_one_district_and_primary_in_another()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        await using var conn = await _sql.OpenConnectionAsync();

        var dualRoleAcrossDistricts = await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM dbo.Salesperson sp
              WHERE EXISTS (SELECT 1 FROM dbo.District d WHERE d.PrimarySalespersonId = sp.Id)
                AND EXISTS (SELECT 1 FROM dbo.DistrictSecondarySalesperson s WHERE s.SalespersonId = sp.Id);");
        Assert.True(dualRoleAcrossDistricts >= 1,
            "expected a salesperson who is primary of one district and secondary of another");
    }

    [SkippableFact]
    public async Task Has_a_district_with_no_stores()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        await using var conn = await _sql.OpenConnectionAsync();

        var emptyDistricts = await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM dbo.District d
              WHERE NOT EXISTS (SELECT 1 FROM dbo.Store s WHERE s.DistrictId = d.Id);");
        Assert.True(emptyDistricts >= 1, "expected at least one district with 0 stores");
    }

    [SkippableFact]
    public async Task Every_salesperson_belongs_to_at_least_one_district()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");
        await using var conn = await _sql.OpenConnectionAsync();

        var unassigned = await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM dbo.Salesperson sp
              WHERE NOT EXISTS (SELECT 1 FROM dbo.District d WHERE d.PrimarySalespersonId = sp.Id)
                AND NOT EXISTS (SELECT 1 FROM dbo.DistrictSecondarySalesperson s WHERE s.SalespersonId = sp.Id);");
        Assert.Equal(0, unassigned); // BR-3
    }

    [SkippableFact]
    public async Task Is_re_runnable_without_changing_the_row_counts()
    {
        Skip.IfNot(_sql.IsAvailable, $"{SqlServerFixture.ConnectionStringEnvVar} is not set.");

        // InitializeAsync already seeded once; a second application must be a no-op on counts.
        await _sql.ApplySeedAsync();

        await using var conn = await _sql.OpenConnectionAsync();
        Assert.Equal(5, await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.District;"));
        Assert.Equal(8, await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Salesperson;"));
        Assert.Equal(17, await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Store;"));
        Assert.Equal(5, await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM dbo.DistrictSecondarySalesperson;"));
    }
}
