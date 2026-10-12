using AeroLink.Web.Data;
using AeroLink.Web.Models;
using AeroLink.Web.Services.Manifests;

namespace AeroLink.Tests;

public class ManifestServiceTests
{
    [Fact]
    public async Task GetFlights_ReturnsAllTenFlights()
    {
        using var testDatabase = new TestDatabase();
        using var databaseContext = testDatabase.CreateContext();
        var manifestService = new ManifestService(databaseContext);

        var flights = await manifestService.GetFlightsAsync();

        Assert.Equal(10, flights.Count);
    }

    [Fact]
    public async Task GetFlights_EarliestDepartureFirst()
    {
        using var testDatabase = new TestDatabase();
        using var databaseContext = testDatabase.CreateContext();
        var manifestService = new ManifestService(databaseContext);

        var flights = await manifestService.GetFlightsAsync();

        for (var i = 1; i < flights.Count; i++)
        {
            Assert.True(flights[i - 1].DepartureTimeUtc <= flights[i].DepartureTimeUtc);
        }
    }

    [Fact]
    public async Task GetFlights_NewSeed_TenExpectedAndNothingLoaded()
    {
        using var testDatabase = new TestDatabase();
        using var databaseContext = testDatabase.CreateContext();
        var manifestService = new ManifestService(databaseContext);

        var flights = await manifestService.GetFlightsAsync();

        Assert.All(flights, flight =>
        {
            Assert.Equal(10, flight.ExpectedBags);
            Assert.Equal(0, flight.LoadedBags);
            Assert.Equal(10, flight.OutstandingBags);
            Assert.Equal("Not started", flight.StatusText);
        });
    }

    [Theory]
    [InlineData(101, 0)]
    [InlineData(102, 2)]
    [InlineData(104, 1)]
    public async Task GetFlights_CountsOpenExceptions(int flightId, int expectedOpen)
    {
        using var testDatabase = new TestDatabase();
        using var databaseContext = testDatabase.CreateContext();
        var manifestService = new ManifestService(databaseContext);

        var flights = await manifestService.GetFlightsAsync();
        var flight = flights.Single(f => f.FlightId == flightId);

        Assert.Equal(expectedOpen, flight.OpenExceptions);
    }

    [Fact]
    public async Task GetManifest_OnlyBagsForThatFlight()
    {
        using var testDatabase = new TestDatabase();
        using var databaseContext = testDatabase.CreateContext();
        var manifestService = new ManifestService(databaseContext);

        var manifest = await manifestService.GetManifestAsync(101);

        Assert.NotNull(manifest);
        Assert.Equal(101, manifest!.Flight.FlightId);
        Assert.Equal(10, manifest.Bags.Count);
        Assert.All(manifest.Bags, bag => Assert.StartsWith("DEMO-101-", bag.Tag));
    }

    [Fact]
    public async Task GetManifest_BagsInTagOrder()
    {
        using var testDatabase = new TestDatabase();
        using var databaseContext = testDatabase.CreateContext();
        var manifestService = new ManifestService(databaseContext);

        var manifest = await manifestService.GetManifestAsync(101);

        Assert.Equal("DEMO-101-001", manifest!.Bags[0].Tag);
        Assert.Equal("DEMO-101-010", manifest.Bags[9].Tag);
    }

    [Fact]
    public async Task GetManifest_ShowsTagTypeAndStatus()
    {
        using var testDatabase = new TestDatabase();
        using var databaseContext = testDatabase.CreateContext();
        var manifestService = new ManifestService(databaseContext);

        var manifest = await manifestService.GetManifestAsync(101);
        var bag = manifest!.Bags.Single(b => b.Tag == "DEMO-101-006");

        Assert.Equal(HandlingType.Fragile, bag.HandlingType);
        Assert.True(bag.IsSpecial);
        Assert.Equal("Handle with care. Keep upright.", bag.HandlingInstruction);
        Assert.Equal("Outstanding", bag.StatusText);
        Assert.Equal("-", bag.LoadedAtText);
    }

    [Fact]
    public async Task GetManifest_StandardBag_IsNotSpecial()
    {
        using var testDatabase = new TestDatabase();
        using var databaseContext = testDatabase.CreateContext();
        var manifestService = new ManifestService(databaseContext);

        var manifest = await manifestService.GetManifestAsync(103);

        Assert.NotNull(manifest);
        Assert.All(manifest!.Bags, bag => Assert.False(bag.IsSpecial));
    }

    [Fact]
    public async Task GetManifest_MarksBagsWithOpenException()
    {
        using var testDatabase = new TestDatabase();
        using var databaseContext = testDatabase.CreateContext();
        var manifestService = new ManifestService(databaseContext);

        var manifest = await manifestService.GetManifestAsync(102);
        var flaggedTags = manifest!.Bags
            .Where(b => b.HasOpenException)
            .Select(b => b.Tag)
            .ToList();

        Assert.Equal(2, flaggedTags.Count);
        Assert.Contains("DEMO-102-001", flaggedTags);
        Assert.Contains("DEMO-102-010", flaggedTags);
    }

    [Fact]
    public async Task GetManifest_SameFlightNumberOnTwoDays_KeptSeparate()
    {
        using var testDatabase = new TestDatabase();
        using var databaseContext = testDatabase.CreateContext();
        var manifestService = new ManifestService(databaseContext);

        var dayOneManifest = await manifestService.GetManifestAsync(101);
        var dayTwoManifest = await manifestService.GetManifestAsync(110);

        Assert.Equal(dayOneManifest!.Flight.FlightNumber, dayTwoManifest!.Flight.FlightNumber);
        Assert.StartsWith("DEMO-101-", dayOneManifest.Bags[0].Tag);
        Assert.StartsWith("DEMO-110-", dayTwoManifest.Bags[0].Tag);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100)]
    [InlineData(111)]
    [InlineData(int.MaxValue)]
    public async Task GetManifest_FlightDoesNotExist_ReturnsNull(int flightId)
    {
        using var testDatabase = new TestDatabase();
        using var databaseContext = testDatabase.CreateContext();
        var manifestService = new ManifestService(databaseContext);

        var manifest = await manifestService.GetManifestAsync(flightId);

        Assert.Null(manifest);
    }

    [Fact]
    public async Task GetManifest_ShowsLoadedAndApprovedBags()
    {
        using var testDatabase = new TestDatabase();
        using (var setup = testDatabase.CreateContext())
        {
            var bagOne = setup.Bags.Single(b => b.Tag == "DEMO-101-001");
            var bagTwo = setup.Bags.Single(b => b.Tag == "DEMO-101-002");
            var bagThree = setup.Bags.Single(b => b.Tag == "DEMO-101-003");

            bagOne.Outcome = BagOutcome.Loaded;
            bagOne.LoadedAtUtc = new DateTime(2026, 10, 5, 2, 30, 0, DateTimeKind.Utc);
            bagTwo.Outcome = BagOutcome.Loaded;
            bagThree.Outcome = BagOutcome.ApprovedNotToLoad;

            setup.SaveChanges();
        }

        using var databaseContext = testDatabase.CreateContext();
        var manifestService = new ManifestService(databaseContext);

        var manifest = await manifestService.GetManifestAsync(101);

        Assert.Equal(2, manifest!.Flight.LoadedBags);
        Assert.Equal(1, manifest.Flight.ApprovedNotToLoadBags);
        Assert.Equal(7, manifest.Flight.OutstandingBags);
        Assert.Equal("Loaded", manifest.Bags[0].StatusText);
        Assert.Equal("12:30", manifest.Bags[0].LoadedAtText);
        Assert.Equal("Approved not to load", manifest.Bags[2].StatusText);
    }

    [Fact]
    public async Task GetManifest_ResolvedException_NoLongerMarked()
    {
        using var testDatabase = new TestDatabase();
        using (var setup = testDatabase.CreateContext())
        {
            var exception = setup.BaggageExceptions.Single(e => e.ExceptionId == 2);
            exception.Status = ExceptionStatus.Resolved;
            setup.SaveChanges();
        }

        using var databaseContext = testDatabase.CreateContext();
        var manifestService = new ManifestService(databaseContext);

        var manifest = await manifestService.GetManifestAsync(104);

        Assert.Equal(0, manifest!.Flight.OpenExceptions);
        Assert.DoesNotContain(manifest.Bags, b => b.HasOpenException);
    }
}
