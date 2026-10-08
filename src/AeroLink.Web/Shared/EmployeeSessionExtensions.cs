namespace AeroLink.Web.Shared;

/// <summary>
/// Reads the signed-in employee from the session, so pages do not repeat the session keys themselves.
/// </summary>
public static class EmployeeSessionExtensions
{
    /// <summary>Gets the employee stored in the session by the sign-in page.</summary>
    /// <param name="session">The current request's session.</param>
    /// <returns>The signed-in employee, or <c>null</c> when nobody is signed in.</returns>
    public static SignedInEmployee? GetSignedInEmployee(this ISession session)
    {
        var employeeId = session.GetInt32(SessionKeys.EmployeeId);
        if (employeeId is null)
        {
            return null;
        }

        return new SignedInEmployee(
            employeeId.Value,
            session.GetString(SessionKeys.EmployeeName) ?? string.Empty,
            session.GetString(SessionKeys.Role) ?? string.Empty);
    }
}