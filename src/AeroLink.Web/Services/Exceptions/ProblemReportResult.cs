namespace AeroLink.Web.Services.Exceptions;

/// <summary>
/// The result of a Baggage Handler reporting a problem with a bag (I4).
/// </summary>
public class ProblemReportResult
{
    /// <summary>True when the report was saved as an open exception.</summary>
    public bool Success { get; set; }

    /// <summary>Why the report was not saved. <see cref="ReportRejection.None"/> when it was.</summary>
    public ReportRejection Rejection { get; set; }

    /// <summary>The message to show the employee.</summary>
    public string Message { get; set; } = "";

    /// <summary>The bag tag in its stored form, e.g. DEMO-101-001. Empty if no tag was entered.</summary>
    public string Tag { get; set; } = "";

    /// <summary>ID of the new exception. Null when the report was not saved.</summary>
    public int? ExceptionId { get; set; }

    /// <summary>When the report was recorded, in UTC. Null when the report was not saved.</summary>
    public DateTime? ReportedAtUtc { get; set; }

    /// <summary>Creates a result for a report that was saved.</summary>
    /// <param name="exceptionId">ID of the new open exception.</param>
    /// <param name="tag">The bag's tag.</param>
    /// <param name="reportedAtUtc">The time recorded on the report, in UTC.</param>
    /// <param name="message">The confirmation to show the employee.</param>
    /// <returns>A result with <see cref="Success"/> set to true.</returns>
    public static ProblemReportResult Recorded(int exceptionId, string tag, DateTime reportedAtUtc, string message)
    {
        return new ProblemReportResult
        {
            Success = true,
            Rejection = ReportRejection.None,
            Message = message,
            Tag = tag,
            ExceptionId = exceptionId,
            ReportedAtUtc = reportedAtUtc
        };
    }

    /// <summary>Creates a result for a report that was not saved. Nothing is changed in the database.</summary>
    /// <param name="rejection">Why the report was refused.</param>
    /// <param name="message">The explanation to show the employee.</param>
    /// <param name="tag">The tag as cleaned up from what was typed.</param>
    /// <returns>A result with <see cref="Success"/> set to false.</returns>
    public static ProblemReportResult Rejected(ReportRejection rejection, string message, string tag)
    {
        return new ProblemReportResult
        {
            Success = false,
            Rejection = rejection,
            Message = message,
            Tag = tag
        };
    }
}
