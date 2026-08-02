using Api.Contracts;
using Api.Services;
using DataAccess;
using Domain;
using Moq;
using Xunit;

namespace Tests.Unit.Api;

/// <summary>
/// Fast unit tests for <see cref="DistrictService"/> with mocked repositories (no database): DTO
/// mapping (incl. store count and the base64 concurrency token), 404s when a referenced resource is
/// absent, domain-conflict surfacing (BR-7), and that persistence is skipped when validation fails.
/// </summary>
[Trait("Category", "Unit")]
public class DistrictServiceTests
{
    private readonly Mock<IDistrictRepository> _districts = new(MockBehavior.Strict);
    private readonly Mock<ISalespersonRepository> _salespersons = new(MockBehavior.Strict);

    private DistrictService NewService() => new(_districts.Object, _salespersons.Object);

    private static readonly byte[] Token = [0, 0, 0, 0, 0, 0, 7, 209]; // "AAAAAAAAB9E="

    [Fact]
    public async Task GetAll_maps_summaries_including_store_count()
    {
        _districts.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new DistrictSummary(1, "North Denmark", new Salesperson(1, "Anna"), 4) });

        var result = await NewService().GetAllAsync();

        var row = Assert.Single(result);
        Assert.Equal("North Denmark", row.Name);
        Assert.Equal("Anna", row.Primary.Name);
        Assert.Equal(4, row.StoreCount);
    }

    [Fact]
    public async Task GetDetail_maps_the_aggregate_and_a_base64_token()
    {
        var district = new District(3, "Copenhagen", new Salesperson(3, "Camilla"),
            secondaries: [new Salesperson(2, "Bjørn")], stores: [new Store(9, "Nørrebro")]);
        _districts.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(district);
        _districts.Setup(r => r.GetRowVersionAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(Token);

        var detail = await NewService().GetDetailAsync(3);

        Assert.Equal("Copenhagen", detail.Name);
        Assert.Equal(3, detail.Primary.Id);
        Assert.Single(detail.Secondaries);
        Assert.Single(detail.Stores);
        Assert.Equal(Convert.ToBase64String(Token), detail.ConcurrencyToken);
    }

    [Fact]
    public async Task GetDetail_throws_NotFound_for_an_unknown_district()
    {
        _districts.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((District?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => NewService().GetDetailAsync(99));
    }

    [Fact]
    public async Task ReplaceAssignments_throws_NotFound_when_the_district_is_unknown()
    {
        _districts.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((District?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            NewService().ReplaceAssignmentsAsync(99, Request(primaryId: 1)));
    }

    [Fact]
    public async Task ReplaceAssignments_throws_NotFound_when_the_primary_is_unknown()
    {
        _districts.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(SeedDistrict());
        _salespersons.Setup(r => r.GetByIdAsync(77, It.IsAny<CancellationToken>())).ReturnsAsync((Salesperson?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            NewService().ReplaceAssignmentsAsync(1, Request(primaryId: 77)));
    }

    [Fact]
    public async Task ReplaceAssignments_rejects_a_secondary_that_equals_the_primary_and_does_not_persist()
    {
        _districts.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(SeedDistrict());
        _salespersons.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Salesperson(5, "Emil"));

        // Same person as both primary and secondary — BR-7.
        await Assert.ThrowsAsync<ConflictException>(() =>
            NewService().ReplaceAssignmentsAsync(1, Request(primaryId: 5, secondaries: [5])));

        _districts.Verify(r => r.ReplaceAssignmentsAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<byte[]>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReplaceAssignments_persists_and_returns_the_new_token()
    {
        var newToken = new byte[] { 0, 0, 0, 0, 0, 0, 8, 1 };
        _districts.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(SeedDistrict());
        _salespersons.Setup(r => r.GetByIdAsync(6, It.IsAny<CancellationToken>())).ReturnsAsync(new Salesperson(6, "Freja"));
        _salespersons.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(new Salesperson(2, "Bjørn"));
        _districts.Setup(r => r.ReplaceAssignmentsAsync(
                1, 6, It.Is<IReadOnlyCollection<int>>(s => s.SequenceEqual(new[] { 2 })), Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newToken);

        var detail = await NewService().ReplaceAssignmentsAsync(1, Request(primaryId: 6, secondaries: [2]));

        Assert.Equal(6, detail.Primary.Id);
        Assert.Contains(detail.Secondaries, s => s.Id == 2);
        Assert.Equal(Convert.ToBase64String(newToken), detail.ConcurrencyToken);
    }

    private static District SeedDistrict() =>
        new(1, "North Denmark", new Salesperson(1, "Anna"), secondaries: null, stores: [new Store(101, "Aalborg")]);

    private static PutAssignmentsRequest Request(int primaryId, IReadOnlyList<int>? secondaries = null) =>
        new(primaryId, secondaries, Convert.ToBase64String(Token));
}
