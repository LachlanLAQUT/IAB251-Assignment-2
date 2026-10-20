using AeroLink.Web.Data;
using AeroLink.Web.Models;
using AeroLink.Web.Shared;

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
    public Task<ProblemReportResult> ReportProblemAsync(int flightId, string? tag, ExceptionCategory category,
        string? comment, SignedInEmployee reporter)
    {
        // Written test-first: the unit tests describing the rules come before the implementation.
        throw new NotImplementedException("Reporting a bag problem has not been implemented yet.");
    }
}
