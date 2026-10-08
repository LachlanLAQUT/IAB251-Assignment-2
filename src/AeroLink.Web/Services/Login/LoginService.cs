using System.Globalization;
using System.Net.Mail;
using AeroLink.Web.Services.Hr;
using AeroLink.Web.Shared;

namespace AeroLink.Web.Services.Login;

/// <summary>
/// Simulated authentication for the prototype: an employee signs in with their HR Employee ID and email,
/// and must hold the Baggage Handler or BG_Supervisor role.
/// </summary>
public class LoginService : ILoginService
{
    private readonly IHrApiClient _hrApiClient;

    /// <summary>Creates the service.</summary>
    /// <param name="hrApiClient">Client used to look employees up in the HR system.</param>
    public LoginService(IHrApiClient hrApiClient)
    {
        _hrApiClient = hrApiClient;
    }

    /// <inheritdoc />
    public async Task<LoginResult> AuthenticateAsync(string? employeeIdText, string? email,
        CancellationToken cancellationToken = default)
    {
        var trimmedEmployeeId = employeeIdText?.Trim() ?? string.Empty;
        var trimmedEmail = email?.Trim() ?? string.Empty;

        // 1. Empty fields.
        if (trimmedEmployeeId.Length == 0)
        {
            return LoginResult.Failure(LoginErrors.EmployeeIdRequired);
        }

        if (trimmedEmail.Length == 0)
        {
            return LoginResult.Failure(LoginErrors.EmailRequired);
        }

        // 2. Data types. NumberStyles.None rejects signs, decimals and spaces, so only digits are accepted.
        if (!int.TryParse(trimmedEmployeeId, NumberStyles.None, CultureInfo.InvariantCulture, out var employeeId)
            || employeeId <= 0)
        {
            return LoginResult.Failure(LoginErrors.EmployeeIdNotANumber);
        }

        if (!IsValidEmailFormat(trimmedEmail))
        {
            return LoginResult.Failure(LoginErrors.EmailInvalidFormat);
        }

        // 3. Check the employee against the HR system.
        HrEmployee? employee;
        try
        {
            employee = await _hrApiClient.GetEmployeeByIdAsync(employeeId, cancellationToken);
        }
        catch (HrApiUnavailableException)
        {
            return LoginResult.Failure(LoginErrors.HrSystemUnavailable);
        }

        // Email addresses are not case-sensitive in practice, so compare ignoring case.
        if (employee is null || !string.Equals(employee.Email, trimmedEmail, StringComparison.OrdinalIgnoreCase))
        {
            return LoginResult.Failure(LoginErrors.CredentialsNotRecognised);
        }

        // 4. Only baggage staff may use the baggage module.
        if (!Roles.IsBaggageRole(employee.JobTitle))
        {
            return LoginResult.Failure(LoginErrors.NoBaggageModuleAccess);
        }

        return LoginResult.Success(employee);
    }

    /// <summary>
    /// Checks the email is a plain address such as name@company.com.
    /// Display-name forms like "Sam &lt;sam@company.com&gt;" are rejected.
    /// </summary>
    private static bool IsValidEmailFormat(string email) =>
        MailAddress.TryCreate(email, out var parsedAddress) && parsedAddress.Address == email;
}