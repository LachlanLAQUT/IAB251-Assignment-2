using AeroLink.Web.Services.Manifests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages.Baggage;

/// <summary>
/// Manifest page (I1). Shows every bag expected on the selected flight with its type and loading status.
/// Address: /Baggage/Manifest?flightId=101
/// </summary>
public class ManifestModel : PageModel
{
    private readonly IManifestService _manifestService;

    /// <summary>Creates the page.</summary>
    /// <param name="manifestService">Service that reads flights and manifests.</param>
    public ManifestModel(IManifestService manifestService)
    {
        _manifestService = manifestService;
    }

    /// <summary>The selected flight, taken from the address.</summary>
    [BindProperty(SupportsGet = true)]
    public int FlightId { get; set; }

    /// <summary>The manifest being shown.</summary>
    public FlightManifest Manifest { get; set; } = new FlightManifest();

    /// <summary>Message passed to the flight list if this flight cannot be shown.</summary>
    [TempData]
    public string? Message { get; set; }

    /// <summary>Loads the manifest, or goes back to the flight list if the flight does not exist.</summary>
    /// <returns>The page, or a redirect to the flight list.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        FlightManifest? manifest = await _manifestService.GetManifestAsync(FlightId);

        if (manifest == null)
        {
            Message = "That flight could not be found. Please choose a flight from the list.";
            return RedirectToPage("./Flights");
        }

        Manifest = manifest;
        return Page();
    }
}
