using AeroLink.Web.Services.Exceptions;
using AeroLink.Web.Services.Manifests;
using AeroLink.Web.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages.Baggage;

/// <summary>
/// Open exceptions page (I5). Lists every baggage exception waiting for a supervisor decision,
/// optionally for one flight only. Supervisors only.
/// Address: /Baggage/Exceptions or /Baggage/Exceptions?flightId=102
/// </summary>
[RequireSupervisor]
public class ExceptionsModel : PageModel
{
    private readonly IExceptionDecisionService _decisionService;
    private readonly IManifestService _manifestService;

    /// <summary>Creates the page.</summary>
    /// <param name="decisionService">Service that reads exceptions and records decisions.</param>
    /// <param name="manifestService">Service that lists flights for the filter.</param>
    public ExceptionsModel(IExceptionDecisionService decisionService, IManifestService manifestService)
    {
        _decisionService = decisionService;
        _manifestService = manifestService;
    }

    /// <summary>The flight to filter by, taken from the address. Null shows every flight.</summary>
    [BindProperty(SupportsGet = true)]
    public int? FlightId { get; set; }

    /// <summary>The open exceptions shown in the table.</summary>
    public List<ExceptionDetails> OpenExceptions { get; private set; } = new List<ExceptionDetails>();

    /// <summary>Flights for the filter list.</summary>
    public List<FlightSummary> Flights { get; private set; } = new List<FlightSummary>();

    /// <summary>Confirmation sent back from the decision page after a decision is saved.</summary>
    [TempData]
    public string? Message { get; set; }

    /// <summary>Loads the open exceptions and the flight filter.</summary>
    public async Task OnGetAsync()
    {
        Flights = await _manifestService.GetFlightsAsync();

        // Ignore a filter for a flight that does not exist rather than showing an empty list.
        if (FlightId != null && !Flights.Any(flight => flight.FlightId == FlightId))
        {
            FlightId = null;
        }

        OpenExceptions = await _decisionService.GetOpenExceptionsAsync(FlightId);
    }
}
