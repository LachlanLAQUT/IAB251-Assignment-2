using System.ComponentModel.DataAnnotations;

namespace AeroLink.Web.Models;

/// <summary>
/// A problem reported against a bag during loading.
/// Open exceptions must be resolved before the bag can be loaded or the flight can be closed.
/// </summary>
public class BaggageException
{
    /// <summary>Primary key for this exception report.</summary>
    public int ExceptionId { get; set; }

    /// <summary>ID of the bag this problem was reported against.</summary>
    public int BagId { get; set; }

    /// <summary>ID of the flight selected when the problem was reported.</summary>
    public int FlightId { get; set; }

    /// <summary>Type of problem.</summary>
    public ExceptionCategory Category { get; set; } = ExceptionCategory.Other;

    /// <summary>Required description of the problem.</summary>
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Current state of the report. New reports start open.</summary>
    public ExceptionStatus Status { get; set; } = ExceptionStatus.Open;

    /// <summary>Report time in UTC.</summary>
    public DateTime ReportedAtUtc { get; set; }

    /// <summary>Employee ID of the person who reported the problem, when available.</summary>
    public int? ReportedByEmployeeId { get; set; }

    /// <summary>Supervisor's explanation, required when approving the bag not to load.</summary>
    [MaxLength(500)]
    public string? DecisionReason { get; set; }

    /// <summary>Decision time in UTC; null while the report is open.</summary>
    public DateTime? DecidedAtUtc { get; set; }

    /// <summary>Employee ID of the supervisor who decided the report, when available.</summary>
    public int? DecidedByEmployeeId { get; set; }

    /// <summary>Bag linked to this exception.</summary>
    public Bag? Bag { get; set; }

    /// <summary>Flight linked to this exception.</summary>
    public Flight? Flight { get; set; }

    /// <summary>True while the exception is awaiting a supervisor decision.</summary>
    public bool IsOpen => Status == ExceptionStatus.Open;
}
