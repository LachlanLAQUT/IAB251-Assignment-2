using AeroLink.Web.Models;

namespace AeroLink.Web.Services.Exceptions;

/// <summary>
/// One baggage exception with the flight and bag details a supervisor needs to make a decision (I5).
/// Used for the list of open exceptions and for the decision page.
/// </summary>
public class ExceptionDetails
{
    /// <summary>ID of the exception report.</summary>
    public int ExceptionId { get; set; }

    /// <summary>ID of the flight the exception belongs to.</summary>
    public int FlightId { get; set; }

    /// <summary>Flight number, e.g. DEMO102.</summary>
    public string FlightNumber { get; set; } = "";

    /// <summary>Route for display, e.g. "BNE to MEL".</summary>
    public string Route { get; set; } = "";

    /// <summary>Scheduled departure time in UTC.</summary>
    public DateTime DepartureTimeUtc { get; set; }

    /// <summary>True when loading for the flight has been closed and nothing can change.</summary>
    public bool IsFlightClosed { get; set; }

    /// <summary>Tag of the bag the problem was reported against.</summary>
    public string Tag { get; set; } = "";

    /// <summary>Handling type of the bag.</summary>
    public HandlingType HandlingType { get; set; }

    /// <summary>Handling instruction for the bag, if it is special.</summary>
    public string HandlingInstruction { get; set; } = "";

    /// <summary>Current outcome of the bag: pending, loaded or approved not to load.</summary>
    public BagOutcome BagOutcome { get; set; }

    /// <summary>Type of problem reported.</summary>
    public ExceptionCategory Category { get; set; }

    /// <summary>The comment entered when the problem was reported.</summary>
    public string Description { get; set; } = "";

    /// <summary>When the problem was reported, in UTC.</summary>
    public DateTime ReportedAtUtc { get; set; }

    /// <summary>Current state of the exception.</summary>
    public ExceptionStatus Status { get; set; }

    /// <summary>The supervisor's reason or note, once a decision has been made.</summary>
    public string? DecisionReason { get; set; }

    /// <summary>When the decision was made, in UTC; null while the exception is open.</summary>
    public DateTime? DecidedAtUtc { get; set; }

    /// <summary>True while the exception is waiting for a supervisor decision.</summary>
    public bool IsOpen
    {
        get { return Status == ExceptionStatus.Open; }
    }

    /// <summary>True when the bag needs special handling.</summary>
    public bool IsSpecial
    {
        get { return HandlingType != HandlingType.Standard; }
    }

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

    /// <summary>Exception status as text, e.g. "Approved not to load".</summary>
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

    /// <summary>Bag type as text, e.g. "Sporting equipment".</summary>
    public string TypeText
    {
        get
        {
            if (HandlingType == HandlingType.SportingEquipment)
            {
                return "Sporting equipment";
            }
            return HandlingType.ToString();
        }
    }

    /// <summary>Departure time in Brisbane time (UTC+10), e.g. "Mon 5 Oct 13:30".</summary>
    public string DepartureTimeText
    {
        get { return DepartureTimeUtc.AddHours(10).ToString("ddd d MMM HH:mm"); }
    }

    /// <summary>Report time in Brisbane time (UTC+10).</summary>
    public string ReportedAtText
    {
        get { return ReportedAtUtc.AddHours(10).ToString("ddd d MMM HH:mm"); }
    }

    /// <summary>Decision time in Brisbane time (UTC+10), or "-" if no decision has been made.</summary>
    public string DecidedAtText
    {
        get
        {
            if (DecidedAtUtc == null)
            {
                return "-";
            }
            return DecidedAtUtc.Value.AddHours(10).ToString("ddd d MMM HH:mm");
        }
    }

    /// <summary>
    /// Builds the details from an exception entity. The exception's <see cref="BaggageException.Bag"/>
    /// and <see cref="BaggageException.Flight"/> must already be loaded.
    /// </summary>
    /// <param name="exception">The exception, with its bag and flight included.</param>
    /// <returns>The details to show on screen.</returns>
    public static ExceptionDetails FromException(BaggageException exception)
    {
        Flight flight = exception.Flight!;
        Bag bag = exception.Bag!;

        return new ExceptionDetails
        {
            ExceptionId = exception.ExceptionId,
            FlightId = flight.FlightId,
            FlightNumber = flight.FlightNumber,
            Route = flight.DepartureAirport + " to " + flight.ArrivalAirport,
            DepartureTimeUtc = flight.ScheduledDepartureUtc,
            IsFlightClosed = flight.LoadingStatus == LoadingStatus.Completed,
            Tag = bag.Tag,
            HandlingType = bag.HandlingType,
            HandlingInstruction = bag.HandlingInstruction,
            BagOutcome = bag.Outcome,
            Category = exception.Category,
            Description = exception.Description,
            ReportedAtUtc = exception.ReportedAtUtc,
            Status = exception.Status,
            DecisionReason = exception.DecisionReason,
            DecidedAtUtc = exception.DecidedAtUtc
        };
    }
}
