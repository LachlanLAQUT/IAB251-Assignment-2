using AeroLink.Web.Data;
using AeroLink.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Web.Pages;

/// <summary>
/// Shows current record counts on the home page.
/// </summary>
public class IndexModel : PageModel
{
    private readonly AppDbContext _databaseContext;

    /// <summary>Creates the page model with access to the application database.</summary>
    /// <param name="databaseContext">The application database context.</param>
    public IndexModel(AppDbContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    /// <summary>Number of flights currently stored.</summary>
    public int FlightCount { get; private set; }

    /// <summary>Number of bags currently stored.</summary>
    public int BagCount { get; private set; }

    /// <summary>Number of exceptions waiting for a decision.</summary>
    public int OpenExceptionCount { get; private set; }

    /// <summary>Loads the latest record counts for the home page.</summary>
    public async Task OnGetAsync()
    {
        FlightCount = await _databaseContext.Flights.CountAsync();
        BagCount = await _databaseContext.Bags.CountAsync();
        OpenExceptionCount = await _databaseContext.BaggageExceptions
            .CountAsync(exception => exception.Status == ExceptionStatus.Open);
    }
}
