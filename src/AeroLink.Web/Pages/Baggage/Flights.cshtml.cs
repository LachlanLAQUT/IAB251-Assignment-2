using AeroLink.Web.Services.Manifests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages.Baggage;

/// <summary>
/// Flight selection page (I1). Lists every departure flight so the employee can pick one.
/// </summary>
public class FlightsModel : PageModel
{
    private readonly IManifestService _manifestService;

    /// <summary>Creates the page.</summary>
    /// <param name="manifestService">Service that reads flights and manifests.</param>
    public FlightsModel(IManifestService manifestService)
    {
        _manifestService = manifestService;
    }

    /// <summary>The flights shown in the table.</summary>
    public List<FlightSummary> Flights { get; set; } = new List<FlightSummary>();

    /// <summary>Message sent back from the manifest page, e.g. when a flight was not found.</summary>
    [TempData]
    public string? Message { get; set; }

    /// <summary>Loads the flights.</summary>
    public async Task OnGetAsync()
    {
        Flights = await _manifestService.GetFlightsAsync();
    }
}
