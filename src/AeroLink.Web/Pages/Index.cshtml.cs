using AeroLink.Web.Services.MainScreen;
using AeroLink.Web.Shared;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AeroLink.Web.Pages;

/// <summary>
/// AeroLink main screen (T1). The entry point for every employee: shows the three modules and a sign-in option.
/// No operational information is shown here, and no module can be opened until the employee has signed in.
/// </summary>
public class IndexModel : PageModel
{
    private readonly IMainScreenService _mainScreenService;

    /// <summary>Creates the page.</summary>
    /// <param name="mainScreenService">Service that decides which modules can be opened.</param>
    public IndexModel(IMainScreenService mainScreenService)
    {
        _mainScreenService = mainScreenService;
    }

    /// <summary>The signed-in employee, or null if nobody is signed in.</summary>
    public SignedInEmployee? Employee { get; set; }

    /// <summary>The module tiles to show.</summary>
    public List<ModuleTile> Modules { get; set; } = new List<ModuleTile>();

    /// <summary>Loads the module tiles for whoever is using the screen.</summary>
    public void OnGet()
    {
        Employee = HttpContext.Session.GetSignedInEmployee();
        Modules = _mainScreenService.GetModules(Employee);
    }
}
