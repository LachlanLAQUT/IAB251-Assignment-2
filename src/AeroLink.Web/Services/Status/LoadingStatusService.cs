using AeroLink.Web.Data;
using AeroLink.Web.Models;
using AeroLink.Web.Services.Manifests;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Web.Services.Status;

/// <summary>
/// Reads a flight, its bags and its exceptions, and turns them into a loading status report (T4).
/// </summary>
public class LoadingStatusService : ILoadingStatusService
{
    private readonly AppDbContext _database;

    /// <summary>Creates the service.</summary>
    /// <param name="database">The application database.</param>
    public LoadingStatusService(AppDbContext database)
    {
        _database = database;
    }

    /// <inheritdoc />
    public async Task<LoadingStatusReport?> GetReportAsync(int flightId)
    {
        if (flightId <= 0)
        {
            return null;
        }

        Flight? flight = await _database.Flights
            .Include(f => f.Bags)
                .ThenInclude(bag => bag.Exceptions)
            .Include(f => f.Exceptions)
                .ThenInclude(exception => exception.Bag)
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.FlightId == flightId);

        if (flight == null)
        {
            return null;
        }

        return BuildReport(flight, DateTime.UtcNow);
    }

    /// <summary>
    /// Builds the report from a flight that already has its bags, the bags' exceptions,
    /// and the flight's exceptions (with their bags) loaded. Kept separate from the database query
    /// so the counting rules are easy to follow and reuse.
    /// </summary>
    /// <param name="flight">The flight with its related data included.</param>
    /// <param name="generatedAtUtc">The time to stamp on the report, in UTC.</param>
    /// <returns>The completed report.</returns>
    public static LoadingStatusReport BuildReport(Flight flight, DateTime generatedAtUtc)
    {
        List<ManifestBag> outstandingBags = flight.Bags
            .Where(bag => bag.Outcome == BagOutcome.Pending)
            .OrderBy(bag => bag.Tag)
            .Select(ManifestBag.FromBag)
            .ToList();

        List<OpenExceptionSummary> openExceptions = flight.Exceptions
            .Where(exception => exception.Status == ExceptionStatus.Open)
            .OrderBy(exception => exception.ReportedAtUtc)
            .ThenBy(exception => exception.ExceptionId)
            .Select(OpenExceptionSummary.FromException)
            .ToList();

        return new LoadingStatusReport
        {
            Flight = FlightSummary.FromFlight(flight),
            OutstandingBags = outstandingBags,
            OpenExceptions = openExceptions,
            GeneratedAtUtc = generatedAtUtc
        };
    }
}
