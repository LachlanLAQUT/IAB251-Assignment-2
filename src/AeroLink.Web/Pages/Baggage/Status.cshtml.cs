using AeroLink.Web.Services.Status;
using AeroLink.Web.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages.Baggage;

/// <summary>
/// Loading status page (T4). Compares the recorded bag outcomes with the flight's manifest, lists what
/// still needs attention, and shows whether loading is ready to be closed.
/// Address: /Baggage/Status?flightId=101
/// </summary>
public class StatusModel : PageModel
{
    /// <summary>How often the page reloads itself while loading is still open, in seconds.</summary>
    public const int RefreshSeconds = 20;

    private readonly ILoadingStatusService _loadingStatusService;

    /// <summary>Creates the page.</summary>
    /// <param name="loadingStatusService">Service that builds the loading status report.</param>
    public StatusModel(ILoadingStatusService loadingStatusService)
    {
        _loadingStatusService = loadingStatusService;
    }

    /// <summary>The selected flight, taken from the address.</summary>
    [BindProperty(SupportsGet = true)]
    public int FlightId { get; set; }

    /// <summary>The report being shown.</summary>
    public LoadingStatusReport Report { get; private set; } = new LoadingStatusReport();

    /// <summary>The employee viewing the page, used to show supervisor wording.</summary>
    public SignedInEmployee Employee { get; private set; } = null!;

    /// <summary>Message passed to the flight list if this flight cannot be shown.</summary>
    [TempData]
    public string? Message { get; set; }

    /// <summary>Loads the report, or goes back to the flight list if the flight does not exist.</summary>
    /// <returns>The page, or a redirect to the flight list.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        LoadingStatusReport? report = await _loadingStatusService.GetReportAsync(FlightId);
        if (report == null)
        {
            Message = "That flight could not be found. Please choose a flight from the list.";
            return RedirectToPage("./Flights");
        }

        // RequireBaggageEmployeeFilter has already sent anyone who is not signed in to the sign-in page.
        Employee = HttpContext.Session.GetSignedInEmployee()!;
        Report = report;
        return Page();
    }
}
