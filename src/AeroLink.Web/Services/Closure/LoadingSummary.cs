using AeroLink.Web.Models;
using AeroLink.Web.Services.Manifests;

namespace AeroLink.Web.Services.Closure;

/// <summary>
/// The final loading summary for a closed flight (I6). It records what happened to every expected bag
/// and every exception, so the completed flight can be reviewed later.
/// </summary>
public class LoadingSummary
{
    /// <summary>The flight and its final counts.</summary>
    public FlightSummary Flight { get; set; } = new FlightSummary();

    /// <summary>When loading was closed, in UTC.</summary>
    public DateTime ClosedAtUtc { get; set; }

    /// <summary>HR employee ID of the supervisor who closed loading.</summary>
    public int? ClosedByEmployeeId { get; set; }

    /// <summary>Bags that were loaded, in tag order.</summary>
    public List<SummaryBag> LoadedBags { get; set; } = new List<SummaryBag>();

    /// <summary>Bags a supervisor approved not to load, with the reason, in tag order.</summary>
    public List<SummaryBag> ApprovedNotToLoadBags { get; set; } = new List<SummaryBag>();

    /// <summary>Every exception reported on the flight and how it was decided, oldest first.</summary>
    public List<SummaryException> Exceptions { get; set; } = new List<SummaryException>();

    /// <summary>Close time in Brisbane time (UTC+10), e.g. "Mon 5 Oct 13:05".</summary>
    public string ClosedAtText
    {
        get { return ClosedAtUtc.AddHours(10).ToString("ddd d MMM HH:mm"); }
    }

    /// <summary>Supervisor who closed loading, for display, e.g. "Employee 200100".</summary>
    public string ClosedByText
    {
        get { return ClosedByEmployeeId == null ? "Unknown" : "Employee " + ClosedByEmployeeId; }
    }
}

/// <summary>One bag on the final loading summary (I6).</summary>
public class SummaryBag
{
    /// <summary>Bag tag, e.g. DEMO-101-001.</summary>
    public string Tag { get; set; } = "";

    /// <summary>Handling type of the bag.</summary>
    public HandlingType HandlingType { get; set; }

    /// <summary>When the bag was loaded, in UTC; null for bags approved not to load.</summary>
    public DateTime? LoadedAtUtc { get; set; }

    /// <summary>HR employee ID of the person who loaded the bag, when it was loaded.</summary>
    public int? LoadedByEmployeeId { get; set; }

    /// <summary>The supervisor's reason, for bags approved not to load.</summary>
    public string? Reason { get; set; }

    /// <summary>True when the bag needs special handling.</summary>
    public bool IsSpecial
    {
        get { return HandlingType != HandlingType.Standard; }
    }

    /// <summary>Bag type as text, e.g. "Sporting equipment".</summary>
    public string TypeText
    {
        get { return HandlingType == HandlingType.SportingEquipment ? "Sporting equipment" : HandlingType.ToString(); }
    }

    /// <summary>Loaded time in Brisbane time (UTC+10), or "-" if the bag was not loaded.</summary>
    public string LoadedAtText
    {
        get { return LoadedAtUtc == null ? "-" : LoadedAtUtc.Value.AddHours(10).ToString("HH:mm"); }
    }

    /// <summary>Who loaded the bag, for display, or "-" if unknown.</summary>
    public string LoadedByText
    {
        get { return LoadedByEmployeeId == null ? "-" : LoadedByEmployeeId.ToString()!; }
    }
}

/// <summary>One exception on the final loading summary, with its decision (I6).</summary>
public class SummaryException
{
    /// <summary>ID of the exception report.</summary>
    public int ExceptionId { get; set; }

    /// <summary>Tag of the bag the problem was reported against.</summary>
    public string Tag { get; set; } = "";

    /// <summary>Type of problem reported.</summary>
    public ExceptionCategory Category { get; set; }

    /// <summary>The comment entered when the problem was reported.</summary>
    public string Description { get; set; } = "";

    /// <summary>How the exception was decided.</summary>
    public ExceptionStatus Status { get; set; }

    /// <summary>The supervisor's reason or note, if one was recorded.</summary>
    public string? DecisionReason { get; set; }

    /// <summary>When the exception was decided, in UTC.</summary>
    public DateTime? DecidedAtUtc { get; set; }

    /// <summary>Problem type as text, e.g. "Missing bag".</summary>
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

    /// <summary>Decision as text, e.g. "Approved not to load".</summary>
    public string StatusText
    {
        get
        {
            switch (Status)
            {
                case ExceptionStatus.Resolved:
                    return "Resolved";
                case ExceptionStatus.ApprovedNotToLoad:
                    return "Approved not to load";
                default:
                    return "Open";
            }
        }
    }

    /// <summary>Decision time in Brisbane time (UTC+10), or "-" if none was recorded.</summary>
    public string DecidedAtText
    {
        get { return DecidedAtUtc == null ? "-" : DecidedAtUtc.Value.AddHours(10).ToString("ddd d MMM HH:mm"); }
    }
}
