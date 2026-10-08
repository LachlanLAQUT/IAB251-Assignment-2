namespace AeroLink.Web.Services.Login;

/// <summary>
/// Error messages shown on the sign-in page. Kept in one place so the page and the unit tests
/// use exactly the same wording.
/// </summary>
public static class LoginErrors
{
    /// <summary>The Employee ID field was left empty.</summary>
    public const string EmployeeIdRequired = "Please enter your Employee ID.";

    /// <summary>The email field was left empty.</summary>
    public const string EmailRequired = "Please enter your email address.";

    /// <summary>The Employee ID was not a positive whole number.</summary>
    public const string EmployeeIdNotANumber = "Employee ID must be a whole number, such as 100101.";

    /// <summary>The email address was not in a valid format.</summary>
    public const string EmailInvalidFormat = "Please enter a valid email address, such as name@company.com.";

    /// <summary>
    /// No employee has that ID, or the email does not match. One message covers both cases
    /// so the page does not reveal which Employee IDs exist.
    /// </summary>
    public const string CredentialsNotRecognised = "The Employee ID and email address do not match our records.";

    /// <summary>The employee exists but is not a Baggage Handler or Baggage Supervisor.</summary>
    public const string NoBaggageModuleAccess =
        "Your role does not have access to the Baggage Unloading, Transfer and Loading Management module.";

    /// <summary>The HR API could not be reached, so the employee could not be checked.</summary>
    public const string HrSystemUnavailable = "The HR system is unavailable right now. Please try again shortly.";
}