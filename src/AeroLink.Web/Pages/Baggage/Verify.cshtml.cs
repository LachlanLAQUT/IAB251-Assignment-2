using AeroLink.Web.Services.Manifests;
using AeroLink.Web.Services.SpecialHandling;
using AeroLink.Web.Services.Verification;
using AeroLink.Web.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages.Baggage;

/// <summary>
/// Verify page (I2). The employee types a bag tag, the system checks it against the flight's manifest,
/// and the employee confirms the bag as loaded.
/// Address: /Baggage/Verify?flightId=101
/// </summary>
public class VerifyModel : PageModel
{
    private readonly IVerificationService _verificationService;
    private readonly IManifestService _manifestService;
    private readonly ISpecialHandlingService _specialHandlingService;

    /// <summary>Creates the page.</summary>
    /// <param name="verificationService">Service that checks tags and loads bags.</param>
    /// <param name="manifestService">Service that reads the flight's progress for the header.</param>
    /// <param name="specialHandlingService">Service that records special-handling acknowledgements (I3).</param>
    public VerifyModel(IVerificationService verificationService, IManifestService manifestService,
        ISpecialHandlingService specialHandlingService)
    {
        _verificationService = verificationService;
        _manifestService = manifestService;
        _specialHandlingService = specialHandlingService;
    }

    /// <summary>The selected flight, taken from the address.</summary>
    [BindProperty(SupportsGet = true)]
    public int FlightId { get; set; }

    /// <summary>The bag tag typed by the employee.</summary>
    [BindProperty]
    public string? Tag { get; set; }

    /// <summary>The flight and its loading progress, shown at the top of the page.</summary>
    public FlightSummary Flight { get; set; } = new FlightSummary();

    /// <summary>The result of the last check or confirm. Null before a tag has been checked.</summary>
    public VerificationResult? Result { get; set; }

    /// <summary>Confirmation shown after a special bag's handling instruction is acknowledged (I3).</summary>
    public string? AcknowledgementMessage { get; set; }

    /// <summary>Message shown after a bag has been loaded.</summary>
    [TempData]
    public string? Message { get; set; }

    /// <summary>Shows the empty tag form.</summary>
    /// <returns>The page, or the flight list if the flight does not exist.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        return await ShowPageAsync();
    }

    /// <summary>Checks the typed tag without saving anything.</summary>
    /// <returns>The page showing whether the bag is OK to load.</returns>
    public async Task<IActionResult> OnPostCheckAsync()
    {
        Result = await _verificationService.CheckTagAsync(FlightId, Tag);
        return await ShowPageAsync();
    }

    /// <summary>
    /// Records that the employee has read a special bag's handling instruction (I3), then shows the bag again
    /// with "Confirm loaded" enabled.
    /// </summary>
    /// <returns>The page showing the bag's updated check result.</returns>
    public async Task<IActionResult> OnPostAcknowledgeAsync()
    {
        // The sign-in filter makes sure someone is signed in on every /Baggage page.
        SignedInEmployee employee = HttpContext.Session.GetSignedInEmployee()!;

        AcknowledgementResult acknowledgement =
            await _specialHandlingService.AcknowledgeHandlingAsync(FlightId, Tag, employee);
        if (acknowledgement.Success)
        {
            AcknowledgementMessage = acknowledgement.Message;
        }

        // Check the bag again so the page shows its current state, including any reason it can no longer load.
        Result = await _verificationService.CheckTagAsync(FlightId, Tag);
        return await ShowPageAsync();
    }

    /// <summary>Confirms the bag as loaded, then starts again with an empty form.</summary>
    /// <returns>A redirect back to this page on success, or the page showing why it failed.</returns>
    public async Task<IActionResult> OnPostConfirmAsync()
    {
        // The sign-in filter makes sure someone is signed in on every /Baggage page.
        SignedInEmployee employee = HttpContext.Session.GetSignedInEmployee()!;

        Result = await _verificationService.ConfirmLoadedAsync(FlightId, Tag, employee.EmployeeId);

        if (Result.Success)
        {
            // Redirect so refreshing the page does not try to load the same bag again.
            Message = Result.Message;
            return RedirectToPage("./Verify", new { flightId = FlightId });
        }

        return await ShowPageAsync();
    }

    /// <summary>Loads the flight for the header, or goes back to the flight list if it does not exist.</summary>
    /// <returns>The page, or a redirect to the flight list.</returns>
    private async Task<IActionResult> ShowPageAsync()
    {
        FlightManifest? manifest = await _manifestService.GetManifestAsync(FlightId);
        if (manifest == null)
        {
            return RedirectToPage("./Flights");
        }

        Flight = manifest.Flight;
        return Page();
    }
}
