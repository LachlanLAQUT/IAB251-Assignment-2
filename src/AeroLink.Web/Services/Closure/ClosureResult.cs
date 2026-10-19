using AeroLink.Web.Services.Status;

namespace AeroLink.Web.Services.Closure;

/// <summary>
/// The result of a supervisor trying to close loading for a flight (I6).
/// </summary>
public class ClosureResult
{
    /// <summary>True when loading was closed.</summary>
    public bool Success { get; set; }

    /// <summary>Why loading was not closed. <see cref="ClosureProblem.None"/> when it was.</summary>
    public ClosureProblem Problem { get; set; }

    /// <summary>The message to show the supervisor.</summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// The loading status at the moment of the attempt. When <see cref="Problem"/> is
    /// <see cref="ClosureProblem.NotReady"/>, its outstanding bags and open exceptions are the items
    /// still blocking closure. Null when the flight was not found or the employee is not a supervisor.
    /// </summary>
    public LoadingStatusReport? Report { get; set; }

    /// <summary>Creates a result for a closure that was refused.</summary>
    /// <param name="problem">Why loading was not closed.</param>
    /// <param name="message">The explanation to show the supervisor.</param>
    /// <param name="report">The current loading status, if the flight was found.</param>
    /// <returns>A result with <see cref="Success"/> set to false.</returns>
    public static ClosureResult Rejected(ClosureProblem problem, string message, LoadingStatusReport? report = null)
    {
        return new ClosureResult
        {
            Success = false,
            Problem = problem,
            Message = message,
            Report = report
        };
    }
}
