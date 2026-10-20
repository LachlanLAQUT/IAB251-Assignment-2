using AeroLink.Web.Models;
using AeroLink.Web.Services.Exceptions;
using AeroLink.Web.Services.Manifests;
using AeroLink.Web.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages.Baggage;

/// <summary>
/// Report a bag problem page (I4). A Baggage Handler records a missing, damaged or otherwise problem bag,
/// which becomes an open exception for the Supervisor.
/// Address: /Baggage/ReportProblem, or /Baggage/ReportProblem?flightId=101&amp;tag=DEMO-101-003 to pre-fill it.
/// </summary>
public class ReportProblemModel : PageModel
{
    private readonly IProblemReportService _problemReportService;
    private readonly IManifestService _manifestService;

    /// <summary>Creates the page.</summary>
    /// <param name="problemReportService">Service that records the report as an open exception.</param>
    /// <param name="manifestService">Service that lists the flights to choose from.</param>
    public ReportProblemModel(IProblemReportService problemReportService, IManifestService manifestService)
    {
        _problemReportService = problemReportService;
        _manifestService = manifestService;
    }

    /// <summary>The selected flight. Can be pre-filled from the address.</summary>
    [BindProperty(SupportsGet = true)]
    public int? FlightId { get; set; }

    /// <summary>The bag tag typed by the employee. Can be pre-filled from the address.</summary>
    [BindProperty(SupportsGet = true)]
    public string? Tag { get; set; }

    /// <summary>The type of problem chosen by the employee.</summary>
    [BindProperty]
    public ExceptionCategory Category { get; set; } = ExceptionCategory.MissingBag;

    /// <summary>What is wrong with the bag, as typed by the employee.</summary>
    [BindProperty]
    public string? Comment { get; set; }

    /// <summary>Flights that are still open for loading, for the flight list.</summary>
    public List<FlightSummary> Flights { get; private set; } = new List<FlightSummary>();

    /// <summary>Why the last report was refused; null when there is nothing to show.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Confirmation shown after a report has been recorded.</summary>
    [TempData]
    public string? Message { get; set; }

    /// <summary>The problem types offered on the form, with the text shown for each.</summary>
    public static IReadOnlyList<(ExceptionCategory Value, string Text)> ProblemTypes { get; } = new[]
    {
        (ExceptionCategory.MissingBag, "Missing bag"),
        (ExceptionCategory.DamagedBag, "Damaged bag"),
        (ExceptionCategory.Other, "Other problem")
    };

    /// <summary>Shows the form, pre-filled with the flight and tag when they are in the address.</summary>
    public async Task OnGetAsync()
    {
        await LoadFlightsAsync();
    }

    /// <summary>Records the report, then shows an empty form for the same flight.</summary>
    /// <returns>A redirect back to this page on success, or the form with the reason it was refused.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        if (FlightId == null)
        {
            ErrorMessage = "Please choose the flight the bag belongs to.";
            await LoadFlightsAsync();
            return Page();
        }

        // A problem type that is not a known name (e.g. a tampered form) cannot be read, and would
        // otherwise silently fall back to Missing bag. Refuse it instead.
        if (ModelState[nameof(Category)]?.ValidationState == ModelValidationState.Invalid)
        {
            ErrorMessage = "Please choose the type of problem.";
            await LoadFlightsAsync();
            return Page();
        }

        // The sign-in filter makes sure someone is signed in on every /Baggage page.
        SignedInEmployee employee = HttpContext.Session.GetSignedInEmployee()!;

        ProblemReportResult result = await _problemReportService.ReportProblemAsync(
            FlightId.Value, Tag, Category, Comment, employee);

        if (!result.Success)
        {
            ErrorMessage = result.Message;
            await LoadFlightsAsync();
            return Page();
        }

        // Redirect so refreshing the page does not report the same problem twice.
        string reportedAt = result.ReportedAtUtc!.Value.AddHours(10).ToString("HH:mm");
        Message = result.Message + " Reported at " + reportedAt + ".";
        return RedirectToPage("./ReportProblem", new { flightId = FlightId });
    }

    /// <summary>Loads the flights that can still have problems reported, earliest departure first.</summary>
    private async Task LoadFlightsAsync()
    {
        List<FlightSummary> flights = await _manifestService.GetFlightsAsync();
        Flights = flights.Where(flight => !flight.IsClosed).ToList();
    }
}
