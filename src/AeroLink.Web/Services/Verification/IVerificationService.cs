namespace AeroLink.Web.Services.Verification;

/// <summary>
/// This service handles the process of checking whether a bag tag is valid
/// for a flight and then loading that bag onto the flight when the user confirms it.
///
/// The name is a bit formal, but the job is pretty simple: validate input,
/// make sure the bag matches the manifest, and then record the result.
/// </summary>
public interface IVerificationService
{
    /// <summary>
    /// Checks whether the supplied bag tag is acceptable for the selected flight.
    /// This is a read-only check. It does not save anything to the database.
    /// </summary>
    /// <param name="flightId">
    /// The flight that the employee is currently working with.
    /// </param>
    /// <param name="tag">
    /// The bag tag typed in by the employee. Lowercase letters and spaces are allowed,
    /// and the method should handle those gracefully.
    /// </param>
    /// <returns>
    /// A result object that tells us if the bag is valid and, if not, why it failed.
    /// </returns>
    Task<VerificationResult> CheckTagAsync(int flightId, string? tag);

    /// <summary>
    /// Confirms that a bag has been loaded onto the flight.
    /// Before loading anything, this method runs the same validation checks again.
    /// That way, if the bag is no longer valid, it will not be marked as loaded.
    ///
    /// If everything passes, the system should mark the bag as loaded, record the
    /// timestamp, save which employee did it, and update the flight status from
    /// "Not started" to "In progress" if needed.
    /// </summary>
    /// <param name="flightId">
    /// The flight the bag is being loaded onto.
    /// </param>
    /// <param name="tag">
    /// The bag tag that the employee is trying to confirm.
    /// </param>
    /// <param name="employeeId">
    /// The HR employee ID of the person who is loading the bag.
    /// </param>
    /// <returns>
    /// A result object telling us whether the load was successful and why it may have failed.
    /// </returns>
    Task<VerificationResult> ConfirmLoadedAsync(int flightId, string? tag, int employeeId);
}
