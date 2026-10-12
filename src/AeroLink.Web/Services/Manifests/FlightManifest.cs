namespace AeroLink.Web.Services.Manifests;

/// <summary>
/// A flight's departure manifest: the flight's details plus every bag expected on it.
/// </summary>
public class FlightManifest
{
    /// <summary>The flight and its loading progress.</summary>
    public FlightSummary Flight { get; set; } = new FlightSummary();

    /// <summary>Every bag expected on the flight, in tag order.</summary>
    public List<ManifestBag> Bags { get; set; } = new List<ManifestBag>();
}
