namespace AeroLink.Web.Shared;

/// <summary>
/// Role names returned by the HR API in an employee's <c>jobTitle</c> field.
/// The checks below ignore capitalization in case the value's casing varies.
/// </summary>
public static class Roles
{
    /// <summary>Role allowed to load bags and report problems.</summary>
    public const string BaggageHandler = "Baggage Handler";

    /// <summary>Role allowed to decide exceptions and close loading.</summary>
    public const string BaggageSupervisor = "BG_Supervisor";

    /// <summary>Checks whether the employee can use the baggage module.</summary>
    /// <param name="jobTitle">The <c>jobTitle</c> value from the HR API. May be null.</param>
    /// <returns>True for either baggage role; otherwise false.</returns>
    public static bool IsBaggageRole(string? jobTitle) =>
        string.Equals(jobTitle, BaggageHandler, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(jobTitle, BaggageSupervisor, StringComparison.OrdinalIgnoreCase);

    /// <summary>Checks whether the employee has the supervisor role.</summary>
    /// <param name="jobTitle">The <c>jobTitle</c> value from the HR API. May be null.</param>
    /// <returns>True for a baggage supervisor; otherwise false.</returns>
    public static bool IsSupervisor(string? jobTitle) =>
        string.Equals(jobTitle, BaggageSupervisor, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Shared session keys keep login, logout, and role checks using the same names.
/// </summary>
public static class SessionKeys
{
    /// <summary>HR employee ID for the signed-in employee.</summary>
    public const string EmployeeId = "EmployeeId";

    /// <summary>Full name of the signed-in employee.</summary>
    public const string EmployeeName = "EmployeeName";

    /// <summary>HR job title of the signed-in employee.</summary>
    public const string Role = "Role";
}
