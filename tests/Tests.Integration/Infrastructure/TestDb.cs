using Dapper;
using Microsoft.Data.SqlClient;

namespace Tests.Integration.Infrastructure;

/// <summary>Small insert helpers so tests can arrange rows in one readable line.</summary>
internal static class TestDb
{
    public static Task<int> InsertSalespersonAsync(this SqlConnection conn, string name = "Primary Person") =>
        conn.QuerySingleAsync<int>(
            "INSERT INTO dbo.Salesperson (Name) VALUES (@name); SELECT CAST(SCOPE_IDENTITY() AS int);",
            new { name });

    public static Task<int> InsertDistrictAsync(this SqlConnection conn, string name, int primaryId) =>
        conn.QuerySingleAsync<int>(
            "INSERT INTO dbo.District (Name, PrimarySalespersonId) VALUES (@name, @primaryId); " +
            "SELECT CAST(SCOPE_IDENTITY() AS int);",
            new { name, primaryId });
}
