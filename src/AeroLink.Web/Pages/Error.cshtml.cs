using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages;

/// <summary>Shows a general error page when an unhandled exception occurs outside Development.</summary>
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    /// <summary>Request ID that can help find the error in server logs.</summary>
    public string? RequestId { get; private set; }

    /// <summary>Sets an ID that can be used to trace this request.</summary>
    public void OnGet()
    {
        // Prefer the activity ID when tracing is available; otherwise use ASP.NET Core's request ID.
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
    }
}
