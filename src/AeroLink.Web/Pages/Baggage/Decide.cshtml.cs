using AeroLink.Web.Services.Exceptions;
using AeroLink.Web.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages.Baggage;

/// <summary>
/// Decision page (I5). Shows one baggage exception and lets the supervisor either resolve the problem
/// or approve the bag not to load with a reason. Supervisors only.
/// Address: /Baggage/Decide?exceptionId=6
/// </summary>
[RequireSupervisor]
public class DecideModel : PageModel
{
    private readonly IExceptionDecisionService _decisionService;

    /// <summary>Creates the page.</summary>
    /// <param name="decisionService">Service that reads exceptions and records decisions.</param>
    public DecideModel(IExceptionDecisionService decisionService)
    {
        _decisionService = decisionService;
    }

    /// <summary>The exception being reviewed, taken from the address or the form.</summary>
    [BindProperty(SupportsGet = true)]
    public int ExceptionId { get; set; }

    /// <summary>The reason or note typed by the supervisor.</summary>
    [BindProperty]
    public string? Reason { get; set; }

    /// <summary>The exception and its flight and bag.</summary>
    public ExceptionDetails Details { get; private set; } = new ExceptionDetails();

    /// <summary>Why the last decision was refused. Null when there is nothing to show.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Message passed to the open exceptions page after a decision or when the exception is missing.</summary>
    [TempData]
    public string? Message { get; set; }

    /// <summary>Shows the exception and the decision form.</summary>
    /// <returns>The page, or the open exceptions list if the exception does not exist.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        return await ShowPageAsync();
    }

    /// <summary>Resolves the problem so the bag can be loaded.</summary>
    /// <returns>A redirect to the open exceptions list on success, or the page showing why it failed.</returns>
    public async Task<IActionResult> OnPostResolveAsync()
    {
        DecisionResult result = await _decisionService.ResolveAsync(ExceptionId, Reason, GetSupervisor());
        return await AfterDecisionAsync(result);
    }

    /// <summary>Approves the bag not to be loaded, with the typed reason.</summary>
    /// <returns>A redirect to the open exceptions list on success, or the page showing why it failed.</returns>
    public async Task<IActionResult> OnPostApproveAsync()
    {
        DecisionResult result = await _decisionService.ApproveNotToLoadAsync(ExceptionId, Reason, GetSupervisor());
        return await AfterDecisionAsync(result);
    }

    /// <summary>
    /// On success, returns to the open exceptions for the same flight so the supervisor can carry on.
    /// On failure, shows this page again with the reason the decision was refused.
    /// </summary>
    /// <param name="result">The result from the decision service.</param>
    /// <returns>A redirect or the page.</returns>
    private async Task<IActionResult> AfterDecisionAsync(DecisionResult result)
    {
        if (!result.Success)
        {
            ErrorMessage = result.Message;
            return await ShowPageAsync();
        }

        Message = result.Message;
        ExceptionDetails? decided = await _decisionService.GetExceptionAsync(ExceptionId);
        return RedirectToPage("./Exceptions", new { flightId = decided?.FlightId });
    }

    /// <summary>Loads the exception, or goes back to the open exceptions list if it does not exist.</summary>
    /// <returns>The page, or a redirect.</returns>
    private async Task<IActionResult> ShowPageAsync()
    {
        ExceptionDetails? details = await _decisionService.GetExceptionAsync(ExceptionId);
        if (details == null)
        {
            Message = "That exception could not be found.";
            return RedirectToPage("./Exceptions");
        }

        Details = details;
        return Page();
    }

    /// <summary>Gets the signed-in employee. The sign-in filter makes sure one is present.</summary>
    /// <returns>The signed-in employee.</returns>
    private SignedInEmployee GetSupervisor()
    {
        return HttpContext.Session.GetSignedInEmployee()!;
    }
}
