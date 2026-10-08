using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages.Account;

/// <summary>
/// Signs the employee out (T3). Logging out only happens through a form post with an
/// anti-forgery token, so another website cannot sign someone out with a hidden link.
/// </summary>
public class LogoutModel : PageModel
{
    /// <summary>Opening the logout address directly does nothing except go to the main screen.</summary>
    /// <returns>A redirect to the main screen.</returns>
    public IActionResult OnGet() => RedirectToPage("/Index");

    /// <summary>Removes the signed-in employee from the session and returns to the sign-in page.</summary>
    /// <returns>A redirect to the sign-in page.</returns>
    public IActionResult OnPost()
    {
        // Clearing the session removes the employee ID, name and role stored at sign-in.
        HttpContext.Session.Clear();

        // TempData survives exactly one redirect, so the message shows once on the sign-in page.
        TempData[LoginModel.StatusMessageKey] = "You have been signed out.";

        return RedirectToPage("/Account/Login");
    }
}