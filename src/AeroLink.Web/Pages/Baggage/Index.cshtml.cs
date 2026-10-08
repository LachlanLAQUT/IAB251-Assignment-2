using AeroLink.Web.Shared;
using Microsoft.AspNetCore.Mvc;
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

    /// <summary>Shows the module's functions, or sends a signed-out user to sign in.</summary>
    /// <returns>The page, or a redirect to the sign-in page.</returns>
    public IActionResult OnGet()
    {
        var employee = HttpContext.Session.GetSignedInEmployee();
        if (employee is null)
        {
            return RedirectToPage("/Account/Login", new { returnUrl = Url.Page("/Baggage/Index") });
        }

        Employee = employee;
        return Page();
    }
}