using AeroLink.Web.Shared;

namespace AeroLink.Web.Services.Closure;

/// <summary>
/// Closes loading for a departure flight and produces its final loading summary (I6).
/// </summary>
public interface ILoadingClosureService
{
    /// <summary>
    /// Closes loading for a flight. This only succeeds when every expected bag is loaded or approved not to
    /// load and no exception is open, using the same rule as the loading status report (T4).
    /// Once closed, the flight is marked Completed with the time and supervisor, and no further loading or
    /// exception decisions are accepted for it.
    /// </summary>
    /// <param name="flightId">The flight to close.</param>
    /// <param name="supervisor">The signed-in employee. Must be a Baggage Supervisor.</param>
    /// <returns>
    /// Whether loading was closed. When it was not ready, the result's report lists the outstanding bags and
    /// open exceptions. Nothing changes when the closure is refused.
    /// </returns>
    Task<ClosureResult> CloseLoadingAsync(int flightId, SignedInEmployee supervisor);

    /// <summary>Gets the final loading summary for a closed flight.</summary>
    /// <param name="flightId">The flight ID.</param>
    /// <returns>The summary, or null if there is no such flight or its loading has not been closed.</returns>
    Task<LoadingSummary?> GetSummaryAsync(int flightId);
}
