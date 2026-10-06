using System.ComponentModel.DataAnnotations;

namespace AeroLink.Web.Models;

/// <summary>
/// A departure flight whose bags are being loaded.
/// Use <see cref="FlightId"/> to identify it because flight numbers can repeat on different days.
/// </summary>
public class Flight
{
    /// <summary>Unique ID for this flight from the manifest.</summary>
    public int FlightId { get; set; }

    /// <summary>Short label used to identify the flight in the manifest.</summary>
    [MaxLength(10)]
    public string FlightLabel { get; set; } = string.Empty;

    /// <summary>Airline flight number. This value can repeat on different days.</summary>
    [MaxLength(20)]
    public string FlightNumber { get; set; } = string.Empty;

    /// <summary>IATA code for the departure airport.</summary>
    [MaxLength(3)]
    public string DepartureAirport { get; set; } = string.Empty;

    /// <summary>IATA code for the arrival airport.</summary>
    [MaxLength(3)]
    public string ArrivalAirport { get; set; } = string.Empty;

    /// <summary>Scheduled departure time, stored in UTC.</summary>
    public DateTime ScheduledDepartureUtc { get; set; }

    /// <summary>ID of the aircraft assigned to this flight.</summary>
    [MaxLength(30)]
    public string AircraftId { get; set; } = string.Empty;

    /// <summary>Current loading state. Completed flights cannot be changed.</summary>
    public LoadingStatus LoadingStatus { get; set; } = LoadingStatus.NotStarted;

    /// <summary>Time loading was closed, in UTC; null until it is closed.</summary>
    public DateTime? LoadingClosedAtUtc { get; set; }

    /// <summary>ID of the supervisor who closed loading, when available.</summary>
    public int? LoadingClosedByEmployeeId { get; set; }

    /// <summary>Bags listed on this flight's departure manifest.</summary>
    public ICollection<Bag> Bags { get; set; } = new List<Bag>();

    /// <summary>Exceptions reported for bags on this flight.</summary>
    public ICollection<BaggageException> Exceptions { get; set; } = new List<BaggageException>();

    /// <summary>True when loading is complete and the flight is locked.</summary>
    public bool IsLoadingClosed => LoadingStatus == LoadingStatus.Completed;

    /// <summary>Flight number and route for display, such as "DEMO101 BNE → SYD".</summary>
    public string DisplayName => $"{FlightNumber} {DepartureAirport} → {ArrivalAirport}";
}
