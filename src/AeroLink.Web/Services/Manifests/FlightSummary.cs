using AeroLink.Web.Models;

namespace AeroLink.Web.Services.Manifests;

/// <summary>
/// One departure flight and how far its loading has got.
/// Shown on the flight selection page and at the top of the manifest page.
/// </summary>
public class FlightSummary
{
    /// <summary>Flight ID from the manifest. Always use this to pick a flight, because flight numbers repeat.</summary>
    public int FlightId { get; set; }

    /// <summary>Flight number, e.g. DEMO101.</summary>
    public string FlightNumber { get; set; } = "";

    /// <summary>Departure airport code, e.g. BNE.</summary>
    public string DepartureAirport { get; set; } = "";

    /// <summary>Arrival airport code, e.g. SYD.</summary>
    public string ArrivalAirport { get; set; } = "";

    /// <summary>Scheduled departure time in UTC.</summary>
    public DateTime DepartureTimeUtc { get; set; }

    /// <summary>Loading status of the flight.</summary>
    public LoadingStatus LoadingStatus { get; set; }

    /// <summary>Number of bags on the manifest.</summary>
    public int ExpectedBags { get; set; }

    /// <summary>Number of bags confirmed as loaded.</summary>
    public int LoadedBags { get; set; }

    /// <summary>Number of bags a supervisor approved not to load.</summary>
    public int ApprovedNotToLoadBags { get; set; }

    /// <summary>Number of exceptions on this flight still waiting for a supervisor decision.</summary>
    public int OpenExceptions { get; set; }

    /// <summary>Bags still to be dealt with: expected minus loaded minus approved not to load.</summary>
    public int OutstandingBags
    {
        get { return ExpectedBags - LoadedBags - ApprovedNotToLoadBags; }
    }

    /// <summary>Route for display, e.g. "BNE to SYD".</summary>
    public string Route
    {
        get { return DepartureAirport + " to " + ArrivalAirport; }
    }

    /// <summary>Departure time in Brisbane time (UTC+10, no daylight saving), e.g. "Mon 5 Oct 13:00".</summary>
    public string DepartureTimeText
    {
        get { return DepartureTimeUtc.AddHours(10).ToString("ddd d MMM HH:mm"); }
    }

    /// <summary>Loading status as text for the screen.</summary>
    public string StatusText
    {
        get
        {
            if (LoadingStatus == LoadingStatus.InProgress)
            {
                return "In progress";
            }
            if (LoadingStatus == LoadingStatus.Completed)
            {
                return "Closed";
            }
            return "Not started";
        }
    }

    /// <summary>True once loading has been closed. The manifest is then read-only.</summary>
    public bool IsClosed
    {
        get { return LoadingStatus == LoadingStatus.Completed; }
    }
}
