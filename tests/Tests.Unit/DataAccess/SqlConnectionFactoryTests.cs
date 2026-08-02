using DataAccess;
using Xunit;

namespace Tests.Unit.DataAccess;

/// <summary>
/// Pure unit tests for the connection factory's construction guard — no database. A factory with no
/// usable connection string is a configuration error we want to surface loudly and early, not a
/// null-reference somewhere downstream.
/// </summary>
[Trait("Category", "Unit")]
public class SqlConnectionFactoryTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructing_without_a_connection_string_is_rejected(string? connectionString)
    {
        Assert.Throws<ArgumentException>(() => new SqlConnectionFactory(connectionString!));
    }

    [Fact]
    public void Constructing_with_a_connection_string_succeeds()
    {
        var factory = new SqlConnectionFactory("Server=.;Database=x;Trusted_Connection=True;");

        Assert.IsAssignableFrom<IDbConnectionFactory>(factory);
    }
}
