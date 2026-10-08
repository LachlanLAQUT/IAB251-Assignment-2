namespace AeroLink.Web.Services.Hr;

/// <summary>
/// Employee details returned by the HR API's <c>GET /api/employees/{id}</c> endpoint.
/// Only the fields AeroLink needs for sign-in are mapped; the HR system remains the owner of this data.
/// </summary>
public class HrEmployee
{
    /// <summary>The employee's HR ID, which employees type in as their Employee ID.</summary>
    public int EmployeeId { get; set; }

    /// <summary>Given name from the HR record.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Family name from the HR record.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Work email address, used as the second sign-in credential.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Job title. AeroLink treats this as the employee's role (see <see cref="Shared.Roles"/>).</summary>
    public string JobTitle { get; set; } = string.Empty;

    /// <summary>Name of the department the employee belongs to.</summary>
    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>First and last name joined for display, such as "Sarah Mitchell".</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();
}