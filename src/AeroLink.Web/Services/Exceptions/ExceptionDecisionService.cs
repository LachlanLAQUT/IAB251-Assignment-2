using AeroLink.Web.Data;
using AeroLink.Web.Models;
using AeroLink.Web.Shared;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Web.Services.Exceptions;

/// <summary>
/// Reads open baggage exceptions and saves supervisor decisions on them (I5).
/// </summary>
public class ExceptionDecisionService : IExceptionDecisionService
{
    /// <summary>Longest reason or note that can be stored, matching the database column.</summary>
    public const int MaxReasonLength = 500;

    private readonly AppDbContext _database;

    /// <summary>Creates the service.</summary>
    /// <param name="database">The application database.</param>
    public ExceptionDecisionService(AppDbContext database)
    {
        _database = database;
    }

    /// <inheritdoc />
    public async Task<List<ExceptionDetails>> GetOpenExceptionsAsync(int? flightId)
    {
        IQueryable<BaggageException> query = _database.BaggageExceptions
            .Include(exception => exception.Bag)
            .Include(exception => exception.Flight)
            .Where(exception => exception.Status == ExceptionStatus.Open);

        if (flightId != null)
        {
            query = query.Where(exception => exception.FlightId == flightId.Value);
        }

        List<BaggageException> exceptions = await query
            .OrderBy(exception => exception.ReportedAtUtc)
            .ThenBy(exception => exception.ExceptionId)
            .AsNoTracking()
            .ToListAsync();

        return exceptions.Select(ExceptionDetails.FromException).ToList();
    }

    /// <inheritdoc />
    public async Task<ExceptionDetails?> GetExceptionAsync(int exceptionId)
    {
        BaggageException? exception = await FindExceptionAsync(exceptionId, asNoTracking: true);
        if (exception == null)
        {
            return null;
        }
        return ExceptionDetails.FromException(exception);
    }

    /// <inheritdoc />
    public async Task<DecisionResult> ResolveAsync(int exceptionId, string? note, SignedInEmployee supervisor)
    {
        CheckOutcome check = await CheckCanDecideAsync(exceptionId, supervisor);
        if (check.Problem != null)
        {
            return check.Problem;
        }

        string cleanNote = CleanText(note);
        if (cleanNote.Length > MaxReasonLength)
        {
            return DecisionResult.Rejected(DecisionProblem.ReasonTooLong,
                "The note must be " + MaxReasonLength + " characters or fewer.");
        }

        BaggageException exception = check.Exception!;

        // The bag's outcome is left as Pending so it can be checked and loaded as normal.
        RecordDecision(exception, ExceptionStatus.Resolved, cleanNote, supervisor);
        await _database.SaveChangesAsync();

        return DecisionResult.Saved("Problem with bag " + exception.Bag!.Tag
            + " resolved. The bag is pending and can now be loaded.");
    }

    /// <inheritdoc />
    public async Task<DecisionResult> ApproveNotToLoadAsync(int exceptionId, string? reason, SignedInEmployee supervisor)
    {
        CheckOutcome check = await CheckCanDecideAsync(exceptionId, supervisor);
        if (check.Problem != null)
        {
            return check.Problem;
        }

        BaggageException exception = check.Exception!;
        Bag bag = exception.Bag!;
        Flight flight = exception.Flight!;

        if (bag.Outcome == BagOutcome.Loaded)
        {
            return DecisionResult.Rejected(DecisionProblem.BagAlreadyLoaded,
                "Bag " + bag.Tag + " has already been loaded, so it cannot be approved not to load. "
                + "Resolve the problem instead.");
        }

        string cleanReason = CleanText(reason);
        if (cleanReason == "")
        {
            return DecisionResult.Rejected(DecisionProblem.ReasonRequired,
                "Enter a reason for approving this bag not to be loaded.");
        }

        if (cleanReason.Length > MaxReasonLength)
        {
            return DecisionResult.Rejected(DecisionProblem.ReasonTooLong,
                "The reason must be " + MaxReasonLength + " characters or fewer.");
        }

        RecordDecision(exception, ExceptionStatus.ApprovedNotToLoad, cleanReason, supervisor);
        bag.Outcome = BagOutcome.ApprovedNotToLoad;

        // A recorded bag outcome means loading work has started on this flight.
        if (flight.LoadingStatus == LoadingStatus.NotStarted)
        {
            flight.LoadingStatus = LoadingStatus.InProgress;
        }

        await _database.SaveChangesAsync();

        return DecisionResult.Saved("Bag " + bag.Tag + " approved not to load. It now counts as accounted for.");
    }

    /// <summary>
    /// Runs the checks shared by both decisions, in order, and stops at the first problem:
    /// the employee is a supervisor, the exception exists, its flight is still open, and it is still open.
    /// </summary>
    /// <param name="exceptionId">The exception being decided.</param>
    /// <param name="supervisor">The signed-in employee.</param>
    /// <returns>The tracked exception (with its bag and flight) when it can be decided, otherwise the problem.</returns>
    private async Task<CheckOutcome> CheckCanDecideAsync(int exceptionId, SignedInEmployee supervisor)
    {
        CheckOutcome outcome = new CheckOutcome();

        if (!supervisor.IsSupervisor)
        {
            outcome.Problem = DecisionResult.Rejected(DecisionProblem.NotSupervisor,
                "Only a Baggage Supervisor can decide baggage exceptions.");
            return outcome;
        }

        BaggageException? exception = await FindExceptionAsync(exceptionId, asNoTracking: false);
        if (exception == null)
        {
            outcome.Problem = DecisionResult.Rejected(DecisionProblem.ExceptionNotFound,
                "That exception could not be found.");
            return outcome;
        }

        if (exception.Flight!.LoadingStatus == LoadingStatus.Completed)
        {
            outcome.Problem = DecisionResult.Rejected(DecisionProblem.LoadingClosed,
                "Loading for " + exception.Flight.FlightNumber + " is closed, so this exception cannot be changed.");
            return outcome;
        }

        if (exception.Status != ExceptionStatus.Open)
        {
            outcome.Problem = DecisionResult.Rejected(DecisionProblem.AlreadyDecided,
                "This exception has already been decided.");
            return outcome;
        }

        outcome.Exception = exception;
        return outcome;
    }

    /// <summary>Loads an exception with its bag and flight.</summary>
    /// <param name="exceptionId">The exception ID. Zero or negative values are treated as not found.</param>
    /// <param name="asNoTracking">True for a read-only load; false when the exception will be changed and saved.</param>
    /// <returns>The exception, or null if there is no exception with that ID.</returns>
    private async Task<BaggageException?> FindExceptionAsync(int exceptionId, bool asNoTracking)
    {
        if (exceptionId <= 0)
        {
            return null;
        }

        IQueryable<BaggageException> query = _database.BaggageExceptions
            .Include(exception => exception.Bag)
            .Include(exception => exception.Flight);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(exception => exception.ExceptionId == exceptionId);
    }

    /// <summary>Sets the decision fields on an exception. Does not save.</summary>
    /// <param name="exception">The tracked exception.</param>
    /// <param name="status">The new status, Resolved or ApprovedNotToLoad.</param>
    /// <param name="reason">The cleaned reason or note. Stored as null when blank.</param>
    /// <param name="supervisor">The supervisor making the decision.</param>
    private static void RecordDecision(BaggageException exception, ExceptionStatus status, string reason,
        SignedInEmployee supervisor)
    {
        exception.Status = status;
        exception.DecisionReason = reason == "" ? null : reason;
        exception.DecidedAtUtc = DateTime.UtcNow;
        exception.DecidedByEmployeeId = supervisor.EmployeeId;
    }

    /// <summary>Removes spaces from both ends of typed text.</summary>
    /// <param name="text">The text as typed. May be null.</param>
    /// <returns>The trimmed text, or "" if nothing was typed.</returns>
    private static string CleanText(string? text)
    {
        return text == null ? "" : text.Trim();
    }

    /// <summary>What <see cref="CheckCanDecideAsync"/> found: either the exception or the problem.</summary>
    private class CheckOutcome
    {
        public BaggageException? Exception { get; set; }
        public DecisionResult? Problem { get; set; }
    }
}
