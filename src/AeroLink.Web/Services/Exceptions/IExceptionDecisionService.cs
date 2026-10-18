using AeroLink.Web.Shared;

namespace AeroLink.Web.Services.Exceptions;

/// <summary>
/// Lets a Baggage Supervisor review open baggage exceptions and record a decision for each (I5).
/// A decision either resolves the problem, so the bag stays pending and can be loaded,
/// or approves the bag not to be loaded, with a reason.
/// </summary>
public interface IExceptionDecisionService
{
    /// <summary>Gets the exceptions still waiting for a decision, oldest first.</summary>
    /// <param name="flightId">Only show this flight's exceptions; null shows every flight.</param>
    /// <returns>The open exceptions. Empty if there are none.</returns>
    Task<List<ExceptionDetails>> GetOpenExceptionsAsync(int? flightId);

    /// <summary>Gets one exception, whether it is open or already decided.</summary>
    /// <param name="exceptionId">The exception ID.</param>
    /// <returns>The exception's details, or null if there is no exception with that ID.</returns>
    Task<ExceptionDetails?> GetExceptionAsync(int exceptionId);

    /// <summary>
    /// Records that the reported problem has been dealt with. The exception becomes Resolved and the bag
    /// stays pending, so it can now be checked and loaded as normal.
    /// </summary>
    /// <param name="exceptionId">The exception to resolve.</param>
    /// <param name="note">Optional note about how the problem was resolved. Blank is allowed.</param>
    /// <param name="supervisor">The signed-in employee. Must be a Baggage Supervisor.</param>
    /// <returns>Whether the decision was saved, and why not if it was refused. Nothing changes when refused.</returns>
    Task<DecisionResult> ResolveAsync(int exceptionId, string? note, SignedInEmployee supervisor);

    /// <summary>
    /// Approves the bag not to be loaded. The exception becomes ApprovedNotToLoad and the bag's outcome
    /// becomes ApprovedNotToLoad, so it counts as accounted for when loading is closed.
    /// </summary>
    /// <param name="exceptionId">The exception to decide.</param>
    /// <param name="reason">Why the bag will not be loaded. Required, up to 500 characters.</param>
    /// <param name="supervisor">The signed-in employee. Must be a Baggage Supervisor.</param>
    /// <returns>Whether the decision was saved, and why not if it was refused. Nothing changes when refused.</returns>
    Task<DecisionResult> ApproveNotToLoadAsync(int exceptionId, string? reason, SignedInEmployee supervisor);
}
