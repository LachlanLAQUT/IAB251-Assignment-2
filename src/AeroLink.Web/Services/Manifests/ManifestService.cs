using AeroLink.Web.Data;
using AeroLink.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Web.Services.Manifests;

/// <summary>
/// Reads flight summaries and passenger manifests from the baggage database.
/// </summary>
public class ManifestService : IManifestService
{
    private readonly AppDbContext _database;

    /// <summary>
    /// Creates a manifest reader for the supplied database context.
    /// </summary>
    public ManifestService(AppDbContext database)
    {
        _database = database;
    }

    /// <inheritdoc />
    public async Task<List<FlightSummary>> GetFlightsAsync()
    {
        var flights = await _database.Flights
            .Include(flight => flight.Bags)
            .Include(flight => flight.Exceptions)
            .OrderBy(flight => flight.ScheduledDepartureUtc)
            .ThenBy(flight => flight.FlightId)
            .AsNoTracking()
            .ToListAsync();

        return flights.Select(CreateSummary).ToList();
    }

    /// <inheritdoc />
    public async Task<FlightManifest?> GetManifestAsync(int flightId)
    {
        if (flightId <= 0)
        {
            return null;
        }

        var flight = await _database.Flights
            .Include(f => f.Bags)
                .ThenInclude(bag => bag.Exceptions)
            .Include(f => f.Exceptions)
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.FlightId == flightId);

        if (flight is null)
        {
            return null;
        }

        return new FlightManifest
        {
            Flight = CreateSummary(flight),
            Bags = flight.Bags
                .OrderBy(bag => bag.Tag)
                .Select(CreateManifestBag)
                .ToList()
        };
    }

    private static FlightSummary CreateSummary(Flight flight)
    {
        return new FlightSummary
        {
            FlightId = flight.FlightId,
            FlightNumber = flight.FlightNumber,
            DepartureAirport = flight.DepartureAirport,
            ArrivalAirport = flight.ArrivalAirport,
            DepartureTimeUtc = flight.ScheduledDepartureUtc,
            LoadingStatus = flight.LoadingStatus,
            ExpectedBags = flight.Bags.Count,
            LoadedBags = flight.Bags.Count(bag => bag.Outcome == BagOutcome.Loaded),
            ApprovedNotToLoadBags = flight.Bags.Count(bag => bag.Outcome == BagOutcome.ApprovedNotToLoad),
            OpenExceptions = flight.Exceptions.Count(exception => exception.Status == ExceptionStatus.Open)
        };
    }

    private static ManifestBag CreateManifestBag(Bag bag)
    {
        return new ManifestBag
        {
            BagId = bag.BagId,
            Tag = bag.Tag,
            HandlingType = bag.HandlingType,
            HandlingInstruction = bag.HandlingInstruction,
            Outcome = bag.Outcome,
            LoadedAtUtc = bag.LoadedAtUtc,
            HasOpenException = bag.Exceptions.Any(exception => exception.Status == ExceptionStatus.Open)
        };
    }
}
