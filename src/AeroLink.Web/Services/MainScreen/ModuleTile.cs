namespace AeroLink.Web.Services.MainScreen;

/// <summary>
/// One functional area (module) shown as a tile on the AeroLink main screen (T1).
/// </summary>
public class ModuleTile
{
    /// <summary>Name of the module, e.g. "Baggage Unloading, Transfer and Loading Management".</summary>
    public string Name { get; set; } = "";

    /// <summary>Short description of what the module is for.</summary>
    public string Description { get; set; } = "";

    /// <summary>The page the tile opens, e.g. "/Baggage/Index". Empty if the module has no pages in this prototype.</summary>
    public string PagePath { get; set; } = "";

    /// <summary>True if the employee can open the module right now.</summary>
    public bool CanOpen { get; set; }

    /// <summary>Text shown on the tile when it cannot be opened, e.g. "Sign in to use this module".</summary>
    public string LockedReason { get; set; } = "";
}
