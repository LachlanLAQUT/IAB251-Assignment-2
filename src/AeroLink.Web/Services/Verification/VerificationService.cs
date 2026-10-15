using AeroLink.Web.Data;
using AeroLink.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Web.Services.Verification;

/// <summary>
/// Checks bag tags against the departure manifest and records bags as loaded (I2).
/// </summary>
public class VerificationService : IVerificationService
{
    private readonly AppDbContext _db;

    /// <summary>Creates the service.</summary>
    /// <param name="db">The application database.</param>
    public VerificationService(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<VerificationResult> CheckTagAsync(int flightId, string? tag)
    {
        CheckOutcome outcome = await CheckAsync(flightId, tag);
        return outcome.Result;
    }

    /// <inheritdoc />
    public async Task<VerificationResult> ConfirmLoadedAsync(int flightId, string? tag, int employeeId)
    {
        // 1. Run every check again. Something may have changed since the tag was first checked.
        CheckOutcome outcome = await CheckAsync(flightId, tag);
        if (!outcome.Result.Success)
        {
            return outcome.Result;
        }

        Bag bag = outcome.Bag!;
        Flight flight = outcome.Flight!;

        // 2. Mark the bag as loaded and record who loaded it and when.
        bag.Outcome = BagOutcome.Loaded;
        bag.LoadedAtUtc = DateTime.UtcNow;
        bag.LoadedByEmployeeId = employeeId;

        // 3. The first bag loaded starts loading for the flight.
        if (flight.LoadingStatus == LoadingStatus.NotStarted)
        {
            flight.LoadingStatus = LoadingStatus.InProgress;
        }

        await _db.SaveChangesAsync();

        outcome.Result.Message = "Bag " + bag.Tag + " confirmed as loaded.";
        return outcome.Result;
    }

    /// <summary>
    /// Runs the checks in order and stops at the first problem.
    /// </summary>
    /// <param name="flightId">The selected flight.</param>
    /// <param name="typedTag">The tag the employee typed.</param>
    /// <returns>The result, plus the bag and flight (tracked, so they can be updated) when the tag is OK.</returns>
    private async Task<CheckOutcome> CheckAsync(int flightId, string? typedTag)
    {
        CheckOutcome outcome = new CheckOutcome();
        string tag = CleanTag(typedTag);

        // 1. A tag must be typed in.
        if (tag == "")
        {
            outcome.Result = VerificationResult.Rejected(VerificationProblem.TagEmpty,
                "Please enter a bag tag.", tag);
            return outcome;
        }

        // 2. The selected flight must exist and still be open for loading.
        Flight? flight = await _db.Flights.FirstOrDefaultAsync(f => f.FlightId == flightId);
        if (flight == null)
        {
            outcome.Result = VerificationResult.Rejected(VerificationProblem.FlightNotFound,
                "The selected flight could not be found.", tag);
            return outcome;
        }

        if (flight.LoadingStatus == LoadingStatus.Completed)
        {
            outcome.Result = VerificationResult.Rejected(VerificationProblem.LoadingClosed,
                "Loading for " + flight.FlightNumber + " is closed. No more bags can be loaded.", tag);
            return outcome;
        }

        // 3. The tag must belong to a bag in the system.
        Bag? bag = await _db.Bags
            .Include(b => b.Flight)
            .Include(b => b.Exceptions)
            .FirstOrDefaultAsync(b => b.Tag == tag);

        if (bag == null)
        {
            outcome.Result = VerificationResult.Rejected(VerificationProblem.TagNotFound,
                "Tag " + tag + " was not found. Check the tag and try again.", tag);
            return outcome;
        }

        // 4. The bag must be on this flight's manifest.
        if (bag.FlightId != flightId)
        {
            // Show the day too, because the same flight number can fly on different days.
            Flight otherFlight = bag.Flight!;
            string otherDay = otherFlight.ScheduledDepartureUtc.AddHours(10).ToString("ddd d MMM");
            outcome.Result = VerificationResult.Rejected(VerificationProblem.WrongFlight,
                "Bag " + tag + " belongs to another flight (" + otherFlight.FlightNumber + " departing " + otherDay
                + "). Do not load it on this flight.", tag);
            return outcome;
        }

        // 5. The bag must not already be accounted for.
        if (bag.Outcome == BagOutcome.Loaded)
        {
            outcome.Result = VerificationResult.Rejected(VerificationProblem.AlreadyLoaded,
                "Bag " + tag + " has already been loaded.", tag);
            return outcome;
        }

        if (bag.Outcome == BagOutcome.ApprovedNotToLoad)
        {
            outcome.Result = VerificationResult.Rejected(VerificationProblem.ApprovedNotToLoad,
                "Bag " + tag + " has been approved not to load by a supervisor.", tag);
            return outcome;
        }

        // 6. The bag must not have a problem waiting for a supervisor decision.
        foreach (BaggageException exception in bag.Exceptions)
        {
            if (exception.Status == ExceptionStatus.Open)
            {
                outcome.Result = VerificationResult.Rejected(VerificationProblem.OpenException,
                    "Bag " + tag + " has an open problem report. A supervisor must decide it before loading.", tag);
                return outcome;
            }
        }

        // All checks passed.
        VerificationResult result = new VerificationResult();
        result.Success = true;
        result.Problem = VerificationProblem.None;
        result.Message = "Bag " + tag + " is on " + flight.FlightNumber + " and is OK to load.";
        result.Tag = tag;
        result.HandlingType = bag.HandlingType;
        result.HandlingInstruction = bag.HandlingInstruction;

        outcome.Result = result;
        outcome.Bag = bag;
        outcome.Flight = flight;
        return outcome;
    }

    /// <summary>
    /// Puts a typed tag into the stored form: no spaces at either end, all capitals.
    /// e.g. "  demo-101-001 " becomes "DEMO-101-001".
    /// </summary>
    /// <param name="typedTag">The tag as typed. May be null.</param>
    /// <returns>The cleaned tag, or "" if nothing was typed.</returns>
    public static string CleanTag(string? typedTag)
    {
        if (typedTag == null)
        {
            return "";
        }
        return typedTag.Trim().ToUpperInvariant();
    }

    /// <summary>What <see cref="CheckAsync"/> found: the result, and the bag and flight when the tag is OK.</summary>
    private class CheckOutcome
    {
        public VerificationResult Result { get; set; } = new VerificationResult();
        public Bag? Bag { get; set; }
        public Flight? Flight { get; set; }
    }
}
