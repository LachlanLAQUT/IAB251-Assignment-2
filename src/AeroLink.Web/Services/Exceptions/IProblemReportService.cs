using AeroLink.Web.Models;
using AeroLink.Web.Shared;

namespace AeroLink.Web.Services.Exceptions;

/// <summary>
/// Lets a Baggage Handler report a problem with a bag during loading (I4).
/// A report becomes an open baggage exception. While it is open, the bag cannot be loaded (I2),
/// the supervisor sees it in the open exceptions list (I5), and the flight cannot be closed (I6).
/// </summary>
public interface IProblemReportService
{
    /// <summary>
    /// Records a problem with a bag on the selected flight as an open baggage exception,
    /// stamped with the current time and the employee who reported it.
    /// </summary>
    /// <param name="flightId">The departure flight the employee is working on.</param>
    /// <param name="tag">The bag tag as typed. Spaces at either end and lowercase letters are allowed.</param>
    /// <param name="category">The type of problem: missing, damaged or other.</param>
    /// <param name="comment">What is wrong with the bag. Required, up to 500 characters.</param>
    /// <param name="reporter">The signed-in employee reporting the problem.</param>
    /// <returns>
    /// The new exception's ID and time when the report was saved, otherwise why it was refused.
    /// Nothing changes when the report is refused.
    /// </returns>
    Task<ProblemReportResult> ReportProblemAsync(int flightId, string? tag, ExceptionCategory category,
        string? comment, SignedInEmployee reporter);
}
