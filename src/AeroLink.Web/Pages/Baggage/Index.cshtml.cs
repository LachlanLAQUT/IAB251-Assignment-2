using AeroLink.Web.Shared;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages.Baggage;

/// <summary>
/// Home page of the Baggage Unloading, Transfer and Loading Management module.
/// Employees land here after signing in and choose the function they need.
/// </summary>
public class IndexModel : PageModel
{
    /// <summary>The employee using the module.</summary>
    public SignedInEmployee Employee { get; private set; } = null!;

    /// <summary>Shows the module's functions for the signed-in employee.</summary>
    public void OnGet()
    {
        // RequireBaggageEmployeeFilter has already sent anyone who is not signed in to the sign-in page.
        Employee = HttpContext.Session.GetSignedInEmployee()!;
    }
}