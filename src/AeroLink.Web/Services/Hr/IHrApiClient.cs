namespace AeroLink.Web.Services.Hr;

/// <summary>
/// Reads employee records from the external HR system through its REST API.
/// The HR system is treated as a black-box service: AeroLink never stores employee records itself.
/// </summary>
public interface IHrApiClient
{
    /// <summary>Looks up a single employee by their HR employee ID.</summary>
    /// <param name="employeeId">The HR employee ID to look up.</param>
    /// <param name="cancellationToken">Cancels the HTTP request.</param>
    /// <returns>The employee, or <c>null</c> when the HR system has no employee with that ID.</returns>
    /// <exception cref="HrApiUnavailableException">The HR API could not be reached or returned an unexpected error.</exception>
    Task<HrEmployee?> GetEmployeeByIdAsync(int employeeId, CancellationToken cancellationToken = default);
}