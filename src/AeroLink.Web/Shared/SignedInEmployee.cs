namespace AeroLink.Web.Shared;

/// <summary>
/// The employee signed in for the current browser session, as stored by the sign-in page.
/// </summary>
/// <param name="EmployeeId">HR employee ID, used when recording who performed an action.</param>
/// <param name="Name">Full name for display.</param>
/// <param name="Role">HR job title, such as "Baggage Handler" or "BG_Supervisor".</param>
public record SignedInEmployee(int EmployeeId, string Name, string Role)
{
    /// <summary>True when the employee is a Baggage Supervisor.</summary>
    public bool IsSupervisor => Roles.IsSupervisor(Role);
}