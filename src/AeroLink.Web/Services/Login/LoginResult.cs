using AeroLink.Web.Services.Hr;

namespace AeroLink.Web.Services.Login;

/// <summary>
/// Outcome of a sign-in attempt: either the verified employee or a message explaining why sign-in failed.
/// </summary>
public sealed class LoginResult
{
    private LoginResult(HrEmployee? employee, string? errorMessage)
    {
        Employee = employee;
        ErrorMessage = errorMessage;
    }

    /// <summary>True when the employee was verified and may use the baggage module.</summary>
    public bool Succeeded => Employee is not null;

    /// <summary>The verified employee from the HR system; null when sign-in failed.</summary>
    public HrEmployee? Employee { get; }

    /// <summary>Message to show the user when sign-in failed; null on success.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Creates a successful result for a verified employee.</summary>
    /// <param name="employee">The employee returned by the HR system.</param>
    /// <returns>A result where <see cref="Succeeded"/> is true.</returns>
    public static LoginResult Success(HrEmployee employee) => new(employee, null);

    /// <summary>Creates a failed result with a message for the user.</summary>
    /// <param name="errorMessage">One of the messages in <see cref="LoginErrors"/>.</param>
    /// <returns>A result where <see cref="Succeeded"/> is false.</returns>
    public static LoginResult Failure(string errorMessage) => new(null, errorMessage);
}