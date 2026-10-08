namespace AeroLink.Web.Services.Login;

/// <summary>
/// Checks an employee's sign-in details against the HR system.
/// It does not touch the session; the sign-in page stores the employee once this succeeds.
/// </summary>
public interface ILoginService
{
    /// <summary>
    /// Validates the typed Employee ID and email, looks the employee up in the HR system,
    /// and confirms they hold a baggage role.
    /// </summary>
    /// <param name="employeeIdText">The Employee ID exactly as typed, which may be empty or not a number.</param>
    /// <param name="email">The email address exactly as typed, which may be empty.</param>
    /// <param name="cancellationToken">Cancels the HR API call.</param>
    /// <returns>The verified employee, or the first problem found as a user-facing message.</returns>
    Task<LoginResult> AuthenticateAsync(string? employeeIdText, string? email,
        CancellationToken cancellationToken = default);
}