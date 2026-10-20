using AeroLink.Web.Data;
using AeroLink.Web.Models;
using AeroLink.Web.Services.Verification;
using AeroLink.Web.Shared;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Web.Services.Exceptions;

/// <summary>
/// Records problems reported by Baggage Handlers as open baggage exceptions (I4).
/// </summary>
public class ProblemReportService : IProblemReportService
{
    /// <summary>Longest comment that can be stored, matching the database column.</summary>
    public const int MaxCommentLength = 500;

    private readonly AppDbContext _database;

    /// <summary>Creates the service.</summary>
    /// <param name="database">The application database.</param>
    public ProblemReportService(AppDbContext database)
    {
        _database = database;
    }

    /// <inheritdoc />
    public async Task<ProblemReportResult> ReportProblemAsync(int flightId, string? tag, ExceptionCategory category,
        string? comment, SignedInEmployee reporter)
    {
        // Use the same tag clean-up as bag verification (I2), so " demo-101-001 " finds DEMO-101-001.
        string cleanTag = VerificationService.CleanTag(tag);
        string cleanComment = comment == null ? "" : comment.Trim();

        // 1. Check what was typed before touching the database.
        if (cleanTag == "")
        {
            return ProblemReportResult.Rejected(ReportRejection.TagEmpty,
                "Please enter the bag tag.", cleanTag);
        }

        if (!Enum.IsDefined(category))
        {
            return ProblemReportResult.Rejected(ReportRejection.CategoryInvalid,
                "Please choose the type of problem.", cleanTag);
        }

        if (cleanComment == "")
        {
            return ProblemReportResult.Rejected(ReportRejection.CommentRequired,
                "Please describe the problem so the Supervisor can investigate it.", cleanTag);
        }

        if (cleanComment.Length > MaxCommentLength)
        {
            return ProblemReportResult.Rejected(ReportRejection.CommentTooLong,
                "The comment must be " + MaxCommentLength + " characters or fewer.", cleanTag);
        }

        // 2. The flight must exist and still be open for loading.
        Flight? flight = await _database.Flights.FirstOrDefaultAsync(f => f.FlightId == flightId);
        if (flight == null)
        {
            return ProblemReportResult.Rejected(ReportRejection.FlightNotFound,
                "The selected flight could not be found.", cleanTag);
        }

        if (flight.LoadingStatus == LoadingStatus.Completed)
        {
            return ProblemReportResult.Rejected(ReportRejection.LoadingClosed,
                "Loading for " + flight.FlightNumber + " is closed, so problems can no longer be reported.", cleanTag);
        }

        // 3. The bag must exist and be on this flight's manifest.
        Bag? bag = await _database.Bags
            .Include(b => b.Exceptions)
            .FirstOrDefaultAsync(b => b.Tag == cleanTag);

        if (bag == null)
        {
            return ProblemReportResult.Rejected(ReportRejection.TagNotFound,
                "Tag " + cleanTag + " was not found. Check the tag and try again.", cleanTag);
        }

        if (bag.FlightId != flightId)
        {
            return ProblemReportResult.Rejected(ReportRejection.WrongFlight,
                "Bag " + cleanTag + " is not on " + flight.FlightNumber
                + "'s manifest. Report it from the flight it belongs to.", cleanTag);
        }

        // 4. Only bags still waiting to be loaded can be reported.
        if (bag.Outcome == BagOutcome.Loaded)
        {
            return ProblemReportResult.Rejected(ReportRejection.BagAlreadyLoaded,
                "Bag " + cleanTag + " has already been loaded, so a problem cannot be reported against it.", cleanTag);
        }

        if (bag.Outcome == BagOutcome.ApprovedNotToLoad)
        {
            return ProblemReportResult.Rejected(ReportRejection.BagApprovedNotToLoad,
                "Bag " + cleanTag + " has already been approved not to load by a Supervisor.", cleanTag);
        }

        // 5. One open report per bag: the Supervisor decides it before another can be raised.
        if (bag.Exceptions.Any(exception => exception.Status == ExceptionStatus.Open))
        {
            return ProblemReportResult.Rejected(ReportRejection.AlreadyReported,
                "Bag " + cleanTag + " already has an open problem report waiting for a Supervisor.", cleanTag);
        }

        // All checks passed: record the problem as an open exception.
        BaggageException report = new BaggageException
        {
            BagId = bag.BagId,
            FlightId = flight.FlightId,
            Category = category,
            Description = cleanComment,
            Status = ExceptionStatus.Open,
            ReportedAtUtc = DateTime.UtcNow,
            ReportedByEmployeeId = reporter.EmployeeId
        };

        _database.BaggageExceptions.Add(report);
        await _database.SaveChangesAsync();

        return ProblemReportResult.Recorded(report.ExceptionId, cleanTag, report.ReportedAtUtc,
            "Problem with bag " + cleanTag + " recorded as exception #" + report.ExceptionId
            + ". The bag cannot be loaded until a Supervisor decides it.");
    }
}
