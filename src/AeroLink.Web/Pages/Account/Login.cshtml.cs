using System.ComponentModel.DataAnnotations;
using AeroLink.Web.Services.Login;
using AeroLink.Web.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages.Account;

/// <summary>
/// Sign-in page (T2). Employees enter their HR Employee ID and email; on success the employee's
/// ID, name and role are stored in the session for the rest of the visit.
/// </summary>
public class LoginModel : PageModel
{
    private readonly ILoginService _loginService;

    /// <summary>Creates the page model.</summary>
    /// <param name="loginService">Service that checks the details against the HR system.</param>
    public LoginModel(ILoginService loginService)
    {
        _loginService = loginService;
    }

    /// <summary>The values typed into the sign-in form.</summary>
    [BindProperty]
    public LoginInput Input { get; set; } = new();

    /// <summary>
    /// Page to return to after signing in, set when a signed-out user was sent here from a protected page.
    /// Only local URLs are followed, so the link cannot redirect to another site.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    /// <summary>TempData key used by other pages, such as Logout, to show a message here after a redirect.</summary>
    public const string StatusMessageKey = "LoginStatusMessage";
    
    /// <summary>Why the last sign-in attempt failed; null when there is nothing to show.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>One-off confirmation passed from another page, such as "You have been signed out."</summary>
    [TempData(Key = StatusMessageKey)]
    public string? StatusMessage { get; set; }

    /// <summary>Shows the form, or skips it when the employee is already signed in.</summary>
    /// <returns>The sign-in page, or a redirect for an employee who is already signed in.</returns>
    public IActionResult OnGet()
    {
        if (HttpContext.Session.GetInt32(SessionKeys.EmployeeId) is not null)
        {
            return RedirectAfterSignIn();
        }

        return Page();
    }

    /// <summary>Checks the typed details and signs the employee in when they are valid.</summary>
    /// <param name="cancellationToken">Cancelled if the browser abandons the request.</param>
    /// <returns>The form with an error message, or a redirect once signed in.</returns>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var result = await _loginService.AuthenticateAsync(Input.EmployeeId, Input.Email, cancellationToken);

        if (!result.Succeeded)
        {
            ErrorMessage = result.ErrorMessage;
            return Page();
        }

        var employee = result.Employee!;

        // Drop anything left from an earlier visit before storing the new employee.
        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32(SessionKeys.EmployeeId, employee.EmployeeId);
        HttpContext.Session.SetString(SessionKeys.EmployeeName, employee.FullName);
        HttpContext.Session.SetString(SessionKeys.Role, employee.JobTitle);

        return RedirectAfterSignIn();
    }

    /// <summary>Goes back to the requested page when it is on this site, otherwise to the baggage module.</summary>
    private IActionResult RedirectAfterSignIn() =>
        Url.IsLocalUrl(ReturnUrl) ? LocalRedirect(ReturnUrl) : RedirectToPage("/Baggage/Index");

    /// <summary>
    /// Raw form values. They are kept as text so <see cref="ILoginService"/> can report empty
    /// or non-numeric values with its own messages.
    /// </summary>
    public class LoginInput
    {
        /// <summary>Employee ID as typed.</summary>
        [Display(Name = "Employee ID")]
        public string? EmployeeId { get; set; }

        /// <summary>Work email address as typed.</summary>
        [Display(Name = "Email address")]
        public string? Email { get; set; }
    }
}