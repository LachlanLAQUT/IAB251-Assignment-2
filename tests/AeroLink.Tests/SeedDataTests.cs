using AeroLink.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Tests;

/// <summary>Checks that the database contains the expected manifest seed data.</summary>
public class SeedDataTests
{
    [Fact]
    public async Task SeedLoadsTenFlights()
    {
        using var testDatabase = new TestDatabase();
        using var databaseContext = testDatabase.CreateContext();

        Assert.Equal(10, await databaseContext.Flights.CountAsync());
    }

    [Fact]
    public async Task SeedLoadsTenBagsPerFlight()
    {
        using var testDatabase = new TestDatabase();
        using var databaseContext = testDatabase.CreateContext();

        var bagCountsByFlight = await databaseContext.Bags
            .GroupBy(bag => bag.FlightId)
            .Select(bagGroup => bagGroup.Count())
            .ToListAsync();

        Assert.Equal(10, bagCountsByFlight.Count);
        Assert.All(bagCountsByFlight, bagCount => Assert.Equal(10, bagCount));
    }

    [Fact]
    public async Task SeededBagsStartAsPending()
    {
        using var testDatabase = new TestDatabase();
        using var databaseContext = testDatabase.CreateContext();

        Assert.True(await databaseContext.Bags.AllAsync(bag => bag.Outcome == BagOutcome.Pending));
    }

    [Fact]
    public async Task SeededExceptionsAreOpenAndMatchTheirBagsFlight()
    {
        using var testDatabase = new TestDatabase();
        using var databaseContext = testDatabase.CreateContext();

        var baggageExceptions = await databaseContext.BaggageExceptions
            .Include(baggageException => baggageException.Bag)
            .ToListAsync();

        Assert.Equal(6, baggageExceptions.Count);
        Assert.All(baggageExceptions, baggageException =>
            Assert.Equal(ExceptionStatus.Open, baggageException.Status));
        Assert.All(baggageExceptions, baggageException =>
            Assert.Equal(baggageException.Bag!.FlightId, baggageException.FlightId));
    }

    [Fact]
    public async Task SpecialBagsHaveHandlingInstructions()
    {
        using var testDatabase = new TestDatabase();
        using var databaseContext = testDatabase.CreateContext();

        var specialBags = await databaseContext.Bags
            .Where(bag => bag.HandlingType != HandlingType.Standard)
            .ToListAsync();

        Assert.NotEmpty(specialBags);
        Assert.All(specialBags, bag =>
            Assert.False(string.IsNullOrWhiteSpace(bag.HandlingInstruction)));
    }

    [Fact]
    public async Task BagTagsMustBeUnique()
    {
        using var testDatabase = new TestDatabase();
        using var databaseContext = testDatabase.CreateContext();

        databaseContext.Bags.Add(new Bag { BagId = 999, FlightId = 101, Tag = "DEMO-101-001" });

        await Assert.ThrowsAsync<DbUpdateException>(() => databaseContext.SaveChangesAsync());
    }

    [Fact]
    public async Task FlightNumbersCanRepeatOnDifferentDays()
    {
        using var testDatabase = new TestDatabase();
        using var databaseContext = testDatabase.CreateContext();

        // The flight ID is needed to distinguish flights with the same number on different days.
        Assert.Equal(2, await databaseContext.Flights
            .CountAsync(flight => flight.FlightNumber == "DEMO101"));
    }
}
