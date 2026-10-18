namespace AeroLink.Web.Services.Exceptions;

/// <summary>
/// The result of a supervisor trying to resolve an exception or approve a bag not to load (I5).
/// </summary>
public class DecisionResult
{
    /// <summary>True when the decision was saved.</summary>
    public bool Success { get; set; }

    /// <summary>Why the decision was not saved. <see cref="DecisionProblem.None"/> when it was.</summary>
    public DecisionProblem Problem { get; set; }

    /// <summary>The message to show the supervisor.</summary>
    public string Message { get; set; } = "";

    /// <summary>Creates a result for a decision that was saved.</summary>
    /// <param name="message">The confirmation to show the supervisor.</param>
    /// <returns>A result with <see cref="Success"/> set to true.</returns>
    public static DecisionResult Saved(string message)
    {
        return new DecisionResult
        {
            Success = true,
            Problem = DecisionProblem.None,
            Message = message
        };
    }

    /// <summary>Creates a result for a decision that was not saved.</summary>
    /// <param name="problem">Why the decision was refused.</param>
    /// <param name="message">The explanation to show the supervisor.</param>
    /// <returns>A result with <see cref="Success"/> set to false.</returns>
    public static DecisionResult Rejected(DecisionProblem problem, string message)
    {
        return new DecisionResult
        {
            Success = false,
            Problem = problem,
            Message = message
        };
    }
}
