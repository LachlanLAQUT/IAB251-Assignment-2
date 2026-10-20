using AeroLink.Web.Shared;

namespace AeroLink.Web.Services.SpecialHandling;

/// <summary>
/// Handles the special-handling step of loading (I3). When a checked bag is special baggage, the employee
/// must acknowledge its handling instruction before it can be confirmed as loaded (enforced by I2's
/// <see cref="Verification.IVerificationService.ConfirmLoadedAsync"/>).
/// </summary>
public interface ISpecialHandlingService
{
    /// <summary>
    /// Records that the employee has read the special bag's handling instruction and will follow it.
    /// The bag must pass the same checks as bag verification (I2) first. Acknowledging a bag twice is allowed.
    /// </summary>
    /// <param name="flightId">The departure flight the employee is loading.</param>
    /// <param name="tag">The bag tag as typed or shown on screen.</param>
    /// <param name="employee">The signed-in employee acknowledging the instruction.</param>
    /// <returns>Whether the instruction is now acknowledged, and why not if it was refused.</returns>
    Task<AcknowledgementResult> AcknowledgeHandlingAsync(int flightId, string? tag, SignedInEmployee employee);
}
