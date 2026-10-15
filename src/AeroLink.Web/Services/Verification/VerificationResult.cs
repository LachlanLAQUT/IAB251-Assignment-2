using AeroLink.Web.Models;

namespace AeroLink.Web.Services.Verification;

/// <summary>
/// This is the result object for checking or confirming a bag tag.
/// It tells us if the tag passed, why it failed if it didn't, and some
/// basic bag details that the page can show to the user.
/// </summary>
public class VerificationResult
{
    /// <summary>
    /// True if the tag passed validation, or if the bag was successfully loaded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// If the result failed, this tells us what kind of problem it was.
    /// If the result succeeded, this should be set to None.
    /// </summary>
    public VerificationProblem Problem { get; set; }

    /// <summary>
    /// The friendly message to show to the employee on screen.
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// The bag tag in the normal format, for example DEMO-101-001.
    /// </summary>
    public string Tag { get; set; } = "";

    /// <summary>
    /// The bag's handling type, if the bag was found in the system.
    /// </summary>
    public HandlingType? HandlingType { get; set; }

    /// <summary>
    /// The bag's handling instruction, if we found the bag.
    /// </summary>
    public string HandlingInstruction { get; set; } = "";

    /// <summary>
    /// This is true when the bag exists and it is not a normal standard bag.
    /// </summary>
    public bool IsSpecial
    {
        get
        {
            if (HandlingType == null)
            {
                return false;
            }

            if (HandlingType == Models.HandlingType.Standard)
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Creates a result that represents a failed validation.
    /// </summary>
    /// <param name="problem">Why the check failed.</param>
    /// <param name="message">The message the employee should see.</param>
    /// <param name="tag">The bag tag that was entered.</param>
    /// <returns>A result with Success set to false.</returns>
    public static VerificationResult Rejected(VerificationProblem problem, string message, string tag)
    {
        VerificationResult result = new VerificationResult();

        result.Success = false;
        result.Problem = problem;
        result.Message = message;
        result.Tag = tag;

        return result;
    }
}
