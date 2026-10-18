using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace AeroLink.Web.Shared;

/// <summary>
/// Put on a page model to limit the page to Baggage Supervisors (used by I5 and I6).
/// A signed-in Baggage Handler is sent back to the baggage module home page with a message.
/// Anyone not signed in is left to <see cref="RequireBaggageEmployeeFilter"/>, which sends them to sign in.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class RequireSupervisorAttribute : Attribute, IPageFilter
{
    /// <summary>TempData key the baggage home page reads to show why the employee was sent there.</summary>
    public const string MessageKey = "Message";

    /// <summary>Not used; the check runs once the handler has been chosen.</summary>
    /// <param name="context">The filter context.</param>
    public void OnPageHandlerSelected(PageHandlerSelectedContext context)
    {
    }

    /// <summary>Redirects a signed-in employee who is not a supervisor before the page runs.</summary>
    /// <param name="context">The filter context; setting its result stops the page from running.</param>
    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        SignedInEmployee? employee = context.HttpContext.Session.GetSignedInEmployee();
        if (employee == null || employee.IsSupervisor)
        {
            return;
        }

        ITempDataDictionaryFactory tempDataFactory =
            context.HttpContext.RequestServices.GetRequiredService<ITempDataDictionaryFactory>();
        tempDataFactory.GetTempData(context.HttpContext)[MessageKey] =
            "Only a Baggage Supervisor can open that page.";

        context.Result = new RedirectToPageResult("/Baggage/Index");
    }

    /// <summary>Not used; nothing needs to happen after the page runs.</summary>
    /// <param name="context">The filter context.</param>
    public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
    {
    }
}
