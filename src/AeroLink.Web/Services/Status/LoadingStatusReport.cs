using AeroLink.Web.Services.Manifests;

namespace AeroLink.Web.Services.Status;

/// <summary>
/// The loading status report for one departure flight (T4). It compares the recorded outcome of every
/// bag with the flight's manifest and says whether loading is ready to be closed.
/// The same readiness rule is used by the supervisor when closing loading (I6).
/// </summary>
public class LoadingStatusReport
{
    /// <summary>The flight and its counts: expected, loaded, approved not to load, outstanding and open exceptions.</summary>
    public FlightSummary Flight { get; set; } = new FlightSummary();

    /// <summary>Bags that are still pending (not loaded and not approved not to load), in tag order.</summary>
    public List<ManifestBag> OutstandingBags { get; set; } = new List<ManifestBag>();

    /// <summary>Exceptions on this flight that are still waiting for a supervisor decision, oldest first.</summary>
    public List<OpenExceptionSummary> OpenExceptions { get; set; } = new List<OpenExceptionSummary>();

    /// <summary>When this report was produced, in UTC.</summary>
    public DateTime GeneratedAtUtc { get; set; }

    /// <summary>Bags with a final outcome: loaded plus approved not to load.</summary>
    public int AccountedForBags
    {
        get { return Flight.LoadedBags + Flight.ApprovedNotToLoadBags; }
    }

    /// <summary>
    /// True when loading can be closed: the flight is still open, every expected bag is loaded or
    /// approved not to load, and no exception is open. A closed flight is never "ready" again.
    /// </summary>
    public bool IsReadyToClose
    {
        get
        {
            return !Flight.IsClosed
                && Flight.OutstandingBags == 0
                && Flight.OpenExceptions == 0;
        }
    }

    /// <summary>
    /// Plain reasons why loading cannot be closed yet, e.g. "3 bags are still outstanding."
    /// Empty when the report is ready to close or the flight is already closed.
    /// </summary>
    public List<string> BlockingReasons
    {
        get
        {
            List<string> reasons = new List<string>();
            if (Flight.IsClosed)
            {
                return reasons;
            }

            if (Flight.OutstandingBags > 0)
            {
                reasons.Add(CountText(Flight.OutstandingBags, "bag is", "bags are") + " still outstanding.");
            }

            if (Flight.OpenExceptions > 0)
            {
                reasons.Add(CountText(Flight.OpenExceptions, "exception is", "exceptions are")
                    + " still open and waiting for a supervisor decision.");
            }

            return reasons;
        }
    }

    /// <summary>Short readiness label for the screen: "Closed", "Ready to close" or "Not ready to close".</summary>
    public string ReadinessText
    {
        get
        {
            if (Flight.IsClosed)
            {
                return "Closed";
            }
            return IsReadyToClose ? "Ready to close" : "Not ready to close";
        }
    }

    /// <summary>Loaded bags as a whole percentage of expected bags, for the progress bar.</summary>
    public int LoadedPercent
    {
        get { return PercentOfExpected(Flight.LoadedBags); }
    }

    /// <summary>Approved not to load bags as a whole percentage of expected bags, for the progress bar.</summary>
    public int ApprovedNotToLoadPercent
    {
        get { return PercentOfExpected(Flight.ApprovedNotToLoadBags); }
    }

    /// <summary>Report time in Brisbane time (UTC+10), e.g. "14:05:32".</summary>
    public string GeneratedAtText
    {
        get { return GeneratedAtUtc.AddHours(10).ToString("HH:mm:ss"); }
    }

    /// <summary>Works out a whole percentage of the expected bags, rounding down.</summary>
    /// <param name="bagCount">Number of bags to express as a percentage.</param>
    /// <returns>0 to 100. Returns 0 when the flight has no expected bags.</returns>
    private int PercentOfExpected(int bagCount)
    {
        if (Flight.ExpectedBags == 0)
        {
            return 0;
        }
        return bagCount * 100 / Flight.ExpectedBags;
    }

    /// <summary>Writes a count with the right singular or plural wording, e.g. "1 bag is" or "3 bags are".</summary>
    /// <param name="count">The number to show.</param>
    /// <param name="singular">Wording used when the count is 1.</param>
    /// <param name="plural">Wording used for any other count.</param>
    /// <returns>The count followed by the matching wording.</returns>
    private static string CountText(int count, string singular, string plural)
    {
        return count + " " + (count == 1 ? singular : plural);
    }
}
