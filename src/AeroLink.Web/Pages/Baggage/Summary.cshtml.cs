using AeroLink.Web.Services.Closure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages.Baggage;

/// <summary>
/// Final loading summary page (I6). Shows the completed loading record for a closed flight.
/// Any baggage employee can open it, so completed flights stay available for review.
/// Address: /Baggage/Summary?flightId=101
/// </summary>
public class SummaryModel : PageModel
{
    private readonly ILoadingClosureService _closureService;

    /// <summary>Creates the page.</summary>
    /// <param name="closureService">Service that builds the final loading summary.</param>
    public SummaryModel(ILoadingClosureService closureService)
    {
        _closureService = closureService;
    }

    /// <summary>The flight, taken from the address.</summary>
    [BindProperty(SupportsGet = true)]
    public int FlightId { get; set; }

    /// <summary>The summary being shown.</summary>
    public LoadingSummary Summary { get; private set; } = new LoadingSummary();

    /// <summary>Confirmation passed from the close page, or a message for the next page after a redirect.</summary>
    [TempData]
    public string? Message { get; set; }

    /// <summary>Loads the summary. A flight that is still open goes to its loading status instead.</summary>
    /// <returns>The page, or a redirect.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        LoadingSummary? summary = await _closureService.GetSummaryAsync(FlightId);
        if (summary == null)
        {
            // Either the flight does not exist (the status page then sends the employee to the flight list)
            // or loading is still open, in which case the status page is the right place to be.
            return RedirectToPage("./Status", new { flightId = FlightId });
        }

        Summary = summary;
        return Page();
    }
}
