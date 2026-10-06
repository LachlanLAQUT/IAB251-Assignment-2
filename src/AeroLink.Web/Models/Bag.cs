using System.ComponentModel.DataAnnotations;

namespace AeroLink.Web.Models;

/// <summary>
/// A bag expected on a departure flight, as listed in the baggage manifest.
/// </summary>
public class Bag
{
    /// <summary>Primary key assigned to this bag in the manifest.</summary>
    public int BagId { get; set; }

    /// <summary>ID of the flight this bag is assigned to.</summary>
    public int FlightId { get; set; }

    /// <summary>Unique tag printed on the bag, such as DEMO-101-001.</summary>
    [Required]
    [MaxLength(30)]
    public string Tag { get; set; } = string.Empty;

    /// <summary>Handling category. Standard means the bag needs no special handling.</summary>
    public HandlingType HandlingType { get; set; } = HandlingType.Standard;

    /// <summary>Instruction shown to the employee when the bag needs special handling.</summary>
    [MaxLength(200)]
    public string HandlingInstruction { get; set; } = string.Empty;

    /// <summary>Current outcome of this bag. Starts as <see cref="BagOutcome.Pending"/>.</summary>
    public BagOutcome Outcome { get; set; } = BagOutcome.Pending;

    /// <summary>Records that an employee has read the special-handling instruction.</summary>
    public bool HandlingAcknowledged { get; set; }

    /// <summary>Load confirmation time in UTC; null until the bag is loaded.</summary>
    public DateTime? LoadedAtUtc { get; set; }

    /// <summary>Employee ID for the load confirmation, kept for accountability.</summary>
    public int? LoadedByEmployeeId { get; set; }

    /// <summary>The flight this bag is manifested on.</summary>
    public Flight? Flight { get; set; }

    /// <summary>Exceptions reported for this bag, including ones that have been decided.</summary>
    public ICollection<BaggageException> Exceptions { get; set; } = new List<BaggageException>();

    /// <summary>True when the bag needs special handling, i.e. its handling type is not Standard.</summary>
    public bool IsSpecial => HandlingType != HandlingType.Standard;

    /// <summary>True when the bag was loaded or approved not to be loaded.</summary>
    public bool IsAccountedFor =>
        Outcome == BagOutcome.Loaded || Outcome == BagOutcome.ApprovedNotToLoad;
}
