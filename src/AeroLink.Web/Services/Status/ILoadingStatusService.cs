namespace AeroLink.Web.Services.Status;

/// <summary>
/// Produces the loading status report for a departure flight (T4). This service only reads data.
/// </summary>
public interface ILoadingStatusService
{
    /// <summary>
    /// Builds an up to date loading status report for one flight from the current database values.
    /// </summary>
    /// <param name="flightId">The flight ID. Zero or negative values are treated as not found.</param>
    /// <returns>The report, or null if there is no flight with that ID.</returns>
    Task<LoadingStatusReport?> GetReportAsync(int flightId);
}
