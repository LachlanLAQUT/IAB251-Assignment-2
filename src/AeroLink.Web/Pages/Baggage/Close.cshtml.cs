using AeroLink.Web.Services.Closure;
using AeroLink.Web.Services.Status;
using AeroLink.Web.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages.Baggage;

/// <summary>
/// Close loading page (I6). Shows whether the flight is ready to close and lists anything still outstanding.
/// When the flight is ready, the supervisor can close loading, which locks the flight and produces the
/// final loading summary. Supervisors only.
/// Address: /Baggage/Close?flightId=101
/// </summary>
[RequireSupervisor]
public class CloseModel : PageModel
{
    private readonly ILoadingClosureService _closureService;
    private readonly ILoadingStatusService _loadingStatusService;

    /// <summary>Creates the page.</summary>
    /// <param name="closureService">Service that closes loading.</param>
    /// <param name="loadingStatusService">Service that builds the loading status report shown before closing.</param>
    public CloseModel(ILoadingClosureService closureService, ILoadingStatusService loadingStatusService)
    {
        _closureService = closureService;
        _loadingStatusService = loadingStatusService;
    }

    /// <summary>The flight to close, taken from the address or the form.</summary>
    [BindProperty(SupportsGet = true)]
    public int FlightId { get; set; }

    /// <summary>The current loading status, including any outstanding bags and open exceptions.</summary>
    public LoadingStatusReport Report { get; private set; } = new LoadingStatusReport();

    /// <summary>Why the last closure attempt was refused. Null when there is nothing to show.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Message passed to the next page after a redirect.</summary>
    [TempData]
    public string? Message { get; set; }

    /// <summary>Shows the readiness check. A flight that is already closed goes straight to its summary.</summary>
    /// <returns>The page, or a redirect.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        LoadingStatusReport? report = await _loadingStatusService.GetReportAsync(FlightId);
        if (report == null)
        {
            Message = "That flight could not be found. Please choose a flight from the list.";
            return RedirectToPage("./Flights");
        }

        if (report.Flight.IsClosed)
        {
            return RedirectToPage("./Summary", new { flightId = FlightId });
        }

        Report = report;
        return Page();
    }

    /// <summary>Tries to close loading. On success, shows the final loading summary.</summary>
    /// <returns>A redirect to the summary, or this page showing what is still outstanding.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        SignedInEmployee supervisor = HttpContext.Session.GetSignedInEmployee()!;
        ClosureResult result = await _closureService.CloseLoadingAsync(FlightId, supervisor);

        if (result.Success || result.Problem == ClosureProblem.AlreadyClosed)
        {
            Message = result.Message;
            return RedirectToPage("./Summary", new { flightId = FlightId });
        }

        if (result.Report == null)
        {
            Message = result.Message;
            return RedirectToPage("./Flights");
        }

        // Not ready: show the latest outstanding items so the supervisor knows what to deal with.
        ErrorMessage = result.Message;
        Report = result.Report;
        return Page();
    }
}
