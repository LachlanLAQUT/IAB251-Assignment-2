using AeroLink.Web.Data;
using AeroLink.Web.Models;
using AeroLink.Web.Services.Manifests;
using AeroLink.Web.Services.Status;
using AeroLink.Web.Shared;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Web.Services.Closure;

/// <summary>
/// Checks readiness, closes loading, and builds the final loading summary (I6).
/// </summary>
public class LoadingClosureService : ILoadingClosureService
{
    private readonly AppDbContext _database;

    /// <summary>Creates the service.</summary>
    /// <param name="database">The application database.</param>
    public LoadingClosureService(AppDbContext database)
    {
        _database = database;
    }

    /// <inheritdoc />
    public async Task<ClosureResult> CloseLoadingAsync(int flightId, SignedInEmployee supervisor)
    {
        // 1. Only a supervisor can close loading.
        if (!supervisor.IsSupervisor)
        {
            return ClosureResult.Rejected(ClosureProblem.NotSupervisor,
                "Only a Baggage Supervisor can close loading.");
        }

        // 2. The flight must exist. It is tracked so it can be updated below.
        Flight? flight = await LoadFlightAsync(flightId, asNoTracking: false);
        if (flight == null)
        {
            return ClosureResult.Rejected(ClosureProblem.FlightNotFound,
                "The selected flight could not be found.");
        }

        // 3. Build the report from the current data, so the readiness rule is the same one T4 shows.
        LoadingStatusReport report = LoadingStatusService.BuildReport(flight, DateTime.UtcNow);

        if (flight.LoadingStatus == LoadingStatus.Completed)
        {
            return ClosureResult.Rejected(ClosureProblem.AlreadyClosed,
                "Loading for " + flight.FlightNumber + " has already been closed.", report);
        }

        // 4. Every bag must be accounted for and no exception may be open.
        if (!report.IsReadyToClose)
        {
            return ClosureResult.Rejected(ClosureProblem.NotReady,
                "Loading for " + flight.FlightNumber + " cannot be closed yet. "
                + string.Join(" ", report.BlockingReasons), report);
        }

        // 5. Close loading and record who closed it and when. Other services refuse changes from now on.
        flight.LoadingStatus = LoadingStatus.Completed;
        flight.LoadingClosedAtUtc = DateTime.UtcNow;
        flight.LoadingClosedByEmployeeId = supervisor.EmployeeId;
        await _database.SaveChangesAsync();

        return new ClosureResult
        {
            Success = true,
            Problem = ClosureProblem.None,
            Message = "Loading for " + flight.FlightNumber + " is closed. The final loading summary has been generated.",
            Report = LoadingStatusService.BuildReport(flight, DateTime.UtcNow)
        };
    }

    /// <inheritdoc />
    public async Task<LoadingSummary?> GetSummaryAsync(int flightId)
    {
        Flight? flight = await LoadFlightAsync(flightId, asNoTracking: true);
        if (flight == null || flight.LoadingStatus != LoadingStatus.Completed)
        {
            return null;
        }

        return BuildSummary(flight);
    }

    /// <summary>
    /// Builds the summary from a closed flight that has its bags (with their exceptions) and its exceptions
    /// (with their bags) loaded.
    /// </summary>
    /// <param name="flight">The closed flight with related data included.</param>
    /// <returns>The final loading summary.</returns>
    public static LoadingSummary BuildSummary(Flight flight)
    {
        List<SummaryBag> loadedBags = flight.Bags
            .Where(bag => bag.Outcome == BagOutcome.Loaded)
            .OrderBy(bag => bag.Tag)
            .Select(bag => new SummaryBag
            {
                Tag = bag.Tag,
                HandlingType = bag.HandlingType,
                LoadedAtUtc = bag.LoadedAtUtc,
                LoadedByEmployeeId = bag.LoadedByEmployeeId
            })
            .ToList();

        List<SummaryBag> approvedBags = flight.Bags
            .Where(bag => bag.Outcome == BagOutcome.ApprovedNotToLoad)
            .OrderBy(bag => bag.Tag)
            .Select(bag => new SummaryBag
            {
                Tag = bag.Tag,
                HandlingType = bag.HandlingType,
                Reason = FindApprovalReason(bag)
            })
            .ToList();

        List<SummaryException> exceptions = flight.Exceptions
            .OrderBy(exception => exception.ReportedAtUtc)
            .ThenBy(exception => exception.ExceptionId)
            .Select(exception => new SummaryException
            {
                ExceptionId = exception.ExceptionId,
                Tag = exception.Bag?.Tag ?? "",
                Category = exception.Category,
                Description = exception.Description,
                Status = exception.Status,
                DecisionReason = exception.DecisionReason,
                DecidedAtUtc = exception.DecidedAtUtc
            })
            .ToList();

        return new LoadingSummary
        {
            Flight = FlightSummary.FromFlight(flight),
            ClosedAtUtc = flight.LoadingClosedAtUtc ?? DateTime.UtcNow,
            ClosedByEmployeeId = flight.LoadingClosedByEmployeeId,
            LoadedBags = loadedBags,
            ApprovedNotToLoadBags = approvedBags,
            Exceptions = exceptions
        };
    }

    /// <summary>
    /// Finds the supervisor's reason for approving a bag not to load, from its most recent approving exception.
    /// </summary>
    /// <param name="bag">The bag, with its exceptions included.</param>
    /// <returns>The reason, or null if none was recorded.</returns>
    private static string? FindApprovalReason(Bag bag)
    {
        return bag.Exceptions
            .Where(exception => exception.Status == ExceptionStatus.ApprovedNotToLoad)
            .OrderByDescending(exception => exception.DecidedAtUtc)
            .Select(exception => exception.DecisionReason)
            .FirstOrDefault();
    }

    /// <summary>Loads a flight with its bags, the bags' exceptions, and its exceptions with their bags.</summary>
    /// <param name="flightId">The flight ID. Zero or negative values are treated as not found.</param>
    /// <param name="asNoTracking">True for a read-only load; false when the flight will be changed and saved.</param>
    /// <returns>The flight, or null if there is no flight with that ID.</returns>
    private async Task<Flight?> LoadFlightAsync(int flightId, bool asNoTracking)
    {
        if (flightId <= 0)
        {
            return null;
        }

        IQueryable<Flight> query = _database.Flights
            .Include(f => f.Bags)
                .ThenInclude(bag => bag.Exceptions)
            .Include(f => f.Exceptions)
                .ThenInclude(exception => exception.Bag);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(f => f.FlightId == flightId);
    }
}
