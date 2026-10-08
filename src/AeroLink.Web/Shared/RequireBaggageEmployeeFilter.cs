using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AeroLink.Web.Shared;

/// <summary>
/// Page filter applied to every page under <c>/Baggage</c> (registered in Program.cs).
/// Anyone not signed in as baggage staff is sent to the sign-in page, then returned to the page they asked for.
/// Pages in the module can therefore assume a signed-in employee is always present.
/// </summary>
public class RequireBaggageEmployeeFilter : IPageFilter
{
    /// <summary>Not used; the check runs once the handler has been chosen.</summary>
    /// <param name="context">The filter context.</param>
    public void OnPageHandlerSelected(PageHandlerSelectedContext context)
    {
    }

    /// <summary>Redirects to sign-in before the page runs when no baggage employee is signed in.</summary>
    /// <param name="context">The filter context; setting its result stops the page from running.</param>
    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var employee = context.HttpContext.Session.GetSignedInEmployee();
        if (employee is not null && Roles.IsBaggageRole(employee.Role))
        {
            return;
        }

        var request = context.HttpContext.Request;
        var returnUrl = $"{request.PathBase}{request.Path}{request.QueryString}";
        context.Result = new RedirectToPageResult("/Account/Login", new { returnUrl });
    }

    /// <summary>Not used; nothing needs to happen after the page runs.</summary>
    /// <param name="context">The filter context.</param>
    public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
    {
    }
}