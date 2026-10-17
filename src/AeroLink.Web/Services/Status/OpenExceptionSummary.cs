using AeroLink.Web.Models;

namespace AeroLink.Web.Services.Status;

/// <summary>
/// One baggage exception that is still waiting for a supervisor decision, as shown on the
/// loading status report (T4).
/// </summary>
public class OpenExceptionSummary
{
    /// <summary>ID of the exception report.</summary>
    public int ExceptionId { get; set; }

    /// <summary>Tag of the bag the problem was reported against, e.g. DEMO-102-001.</summary>
    public string Tag { get; set; } = "";

    /// <summary>Type of problem that was reported.</summary>
    public ExceptionCategory Category { get; set; }

    /// <summary>The comment entered when the problem was reported.</summary>
    public string Description { get; set; } = "";

    /// <summary>When the problem was reported, in UTC.</summary>
    public DateTime ReportedAtUtc { get; set; }

    /// <summary>Problem type as text for the screen, e.g. "Missing bag".</summary>
    public string CategoryText
    {
        get
        {
            switch (Category)
            {
                case ExceptionCategory.MissingBag:
                    return "Missing bag";
                case ExceptionCategory.DamagedBag:
                    return "Damaged bag";
                default:
                    return "Other problem";
            }
        }
    }

    /// <summary>Report time in Brisbane time (UTC+10), e.g. "Mon 5 Oct 12:00".</summary>
    public string ReportedAtText
    {
        get { return ReportedAtUtc.AddHours(10).ToString("ddd d MMM HH:mm"); }
    }

    /// <summary>
    /// Builds a summary from an exception entity. The exception's <see cref="BaggageException.Bag"/>
    /// should be loaded so the tag can be shown.
    /// </summary>
    /// <param name="exception">The exception, with its bag included.</param>
    /// <returns>The row to show on the status report. The tag is empty if the bag was not loaded.</returns>
    public static OpenExceptionSummary FromException(BaggageException exception)
    {
        return new OpenExceptionSummary
        {
            ExceptionId = exception.ExceptionId,
            Tag = exception.Bag?.Tag ?? "",
            Category = exception.Category,
            Description = exception.Description,
            ReportedAtUtc = exception.ReportedAtUtc
        };
    }
}
