using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AeroLink.Web.Services.Hr;

/// <summary>
/// Calls the supplied HR system's REST API using a typed <see cref="HttpClient"/>.
/// The base address is configured in Program.cs from <c>HrApi:BaseUrl</c> in appsettings.json.
/// </summary>
public class HrApiClient : IHrApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HrApiClient> _logger;

    /// <summary>Creates the client. ASP.NET Core supplies the configured <see cref="HttpClient"/>.</summary>
    /// <param name="httpClient">HTTP client whose base address points at the HR API.</param>
    /// <param name="logger">Logs failed calls so connection problems can be diagnosed.</param>
    public HrApiClient(HttpClient httpClient, ILogger<HrApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<HrEmployee?> GetEmployeeByIdAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync($"api/employees/{employeeId}", cancellationToken);
        }
        catch (HttpRequestException requestError)
        {
            _logger.LogError(requestError, "Could not reach the HR API at {BaseAddress}.", _httpClient.BaseAddress);
            throw new HrApiUnavailableException("The HR system could not be reached.", requestError);
        }
        catch (TaskCanceledException timeout) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient reports a timeout as a cancelled task, not as an HttpRequestException.
            _logger.LogError(timeout, "The HR API at {BaseAddress} timed out.", _httpClient.BaseAddress);
            throw new HrApiUnavailableException("The HR system did not respond in time.", timeout);
        }

        using (response)
        {
            // 404 is an expected answer ("no such employee"), not an error.
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("The HR API returned status {StatusCode} for employee {EmployeeId}.",
                    (int)response.StatusCode, employeeId);
                throw new HrApiUnavailableException($"The HR system returned status {(int)response.StatusCode}.");
            }

            try
            {
                // ReadFromJsonAsync uses web defaults, so "firstName" maps to FirstName.
                return await response.Content.ReadFromJsonAsync<HrEmployee>(cancellationToken);
            }
            catch (JsonException parseError)
            {
                _logger.LogError(parseError, "The HR API returned an unreadable employee record.");
                throw new HrApiUnavailableException("The HR system returned an unreadable response.", parseError);
            }
        }
    }
}