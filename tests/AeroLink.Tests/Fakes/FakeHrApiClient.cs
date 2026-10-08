using AeroLink.Web.Services.Hr;

namespace AeroLink.Tests.Fakes;

/// <summary>
/// Stand-in for the HR API so login tests run without the real HR system.
/// It holds a small set of employees and can be told to act as if the HR system is down.
/// </summary>
public class FakeHrApiClient : IHrApiClient
{
    private readonly Dictionary<int, HrEmployee> _employeesById = new();

    /// <summary>When true, every lookup throws as if the HR API could not be reached.</summary>
    public bool SimulateOutage { get; set; }

    /// <summary>Number of lookups made, so tests can check the HR API was not called needlessly.</summary>
    public int LookupCount { get; private set; }

    /// <summary>Adds an employee that lookups will return.</summary>
    /// <param name="employeeId">HR employee ID.</param>
    /// <param name="email">Work email address.</param>
    /// <param name="jobTitle">Job title, which AeroLink treats as the role.</param>
    /// <param name="firstName">Given name.</param>
    /// <param name="lastName">Family name.</param>
    /// <returns>This fake, so calls can be chained.</returns>
    public FakeHrApiClient WithEmployee(int employeeId, string email, string jobTitle,
        string firstName = "Test", string lastName = "Employee")
    {
        _employeesById[employeeId] = new HrEmployee
        {
            EmployeeId = employeeId,
            Email = email,
            JobTitle = jobTitle,
            FirstName = firstName,
            LastName = lastName
        };
        return this;
    }

    /// <inheritdoc />
    public Task<HrEmployee?> GetEmployeeByIdAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        LookupCount++;

        if (SimulateOutage)
        {
            throw new HrApiUnavailableException("Simulated HR outage.");
        }

        _employeesById.TryGetValue(employeeId, out var employee);
        return Task.FromResult(employee);
    }
}