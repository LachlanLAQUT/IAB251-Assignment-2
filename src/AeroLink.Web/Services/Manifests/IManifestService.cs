namespace AeroLink.Web.Services.Manifests;

/// <summary>
/// Reads departure flights and their baggage manifests (I1). This service never changes any data.
/// </summary>
public interface IManifestService
{
    /// <summary>Gets every departure flight with its loading progress, earliest departure first.</summary>
    /// <returns>A list of flights. Empty if there are no flights.</returns>
    Task<List<FlightSummary>> GetFlightsAsync();

    /// <summary>Gets the manifest for one flight.</summary>
    /// <param name="flightId">The flight ID.</param>
    /// <returns>The flight and its bags, or null if there is no flight with that ID.</returns>
    Task<FlightManifest?> GetManifestAsync(int flightId);
}
