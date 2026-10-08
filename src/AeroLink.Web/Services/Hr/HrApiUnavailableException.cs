namespace AeroLink.Web.Services.Hr;

/// <summary>
/// Thrown when the HR API cannot be reached or responds with an unexpected error,
/// so callers can tell "HR is down" apart from "employee not found".
/// </summary>
public class HrApiUnavailableException : Exception
{
    /// <summary>Creates the exception with a message and the underlying cause.</summary>
    /// <param name="message">Description of what went wrong.</param>
    /// <param name="innerException">The original HTTP or parsing error, if any.</param>
    public HrApiUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}