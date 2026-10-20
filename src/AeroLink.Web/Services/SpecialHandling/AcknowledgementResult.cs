using AeroLink.Web.Models;
using AeroLink.Web.Services.Verification;

namespace AeroLink.Web.Services.SpecialHandling;

/// <summary>
/// The result of a Baggage Handler acknowledging a special bag's handling instruction (I3).
/// </summary>
public class AcknowledgementResult
{
    /// <summary>True when the instruction is now acknowledged, so the bag can be confirmed as loaded.</summary>
    public bool Success { get; set; }

    /// <summary>Why the acknowledgement was not recorded. <see cref="AcknowledgementRejection.None"/> when it was.</summary>
    public AcknowledgementRejection Rejection { get; set; }

    /// <summary>
    /// The bag verification problem (I2) when <see cref="Rejection"/> is
    /// <see cref="AcknowledgementRejection.BagNotAccepted"/>; otherwise <see cref="VerificationProblem.None"/>.
    /// </summary>
    public VerificationProblem BagProblem { get; set; }

    /// <summary>The message to show the employee.</summary>
    public string Message { get; set; } = "";

    /// <summary>The bag tag in its stored form, e.g. DEMO-101-006.</summary>
    public string Tag { get; set; } = "";

    /// <summary>True when the instruction had already been acknowledged before this request.</summary>
    public bool WasAlreadyAcknowledged { get; set; }

    /// <summary>Creates a result for an instruction that is now acknowledged.</summary>
    /// <param name="tag">The bag's tag.</param>
    /// <param name="wasAlreadyAcknowledged">True if it had been acknowledged earlier.</param>
    /// <param name="message">The confirmation to show the employee.</param>
    /// <returns>A result with <see cref="Success"/> set to true.</returns>
    public static AcknowledgementResult Acknowledged(string tag, bool wasAlreadyAcknowledged, string message)
    {
        return new AcknowledgementResult
        {
            Success = true,
            Rejection = AcknowledgementRejection.None,
            BagProblem = VerificationProblem.None,
            Message = message,
            Tag = tag,
            WasAlreadyAcknowledged = wasAlreadyAcknowledged
        };
    }

    /// <summary>Creates a result for an acknowledgement that was not recorded. Nothing is changed.</summary>
    /// <param name="rejection">Why it was refused.</param>
    /// <param name="bagProblem">The bag verification problem, if that was the reason.</param>
    /// <param name="message">The explanation to show the employee.</param>
    /// <param name="tag">The tag as cleaned up from what was typed.</param>
    /// <returns>A result with <see cref="Success"/> set to false.</returns>
    public static AcknowledgementResult Rejected(AcknowledgementRejection rejection, VerificationProblem bagProblem,
        string message, string tag)
    {
        return new AcknowledgementResult
        {
            Success = false,
            Rejection = rejection,
            BagProblem = bagProblem,
            Message = message,
            Tag = tag
        };
    }
}
