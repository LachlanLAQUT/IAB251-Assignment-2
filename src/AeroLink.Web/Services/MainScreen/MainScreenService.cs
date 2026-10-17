using AeroLink.Web.Shared;

namespace AeroLink.Web.Services.MainScreen;

/// <summary>
/// Builds the module tiles for the AeroLink main screen (T1).
/// Only the baggage module is part of this prototype; the other two are shown so the employee
/// can see the whole system, but they cannot be opened.
/// </summary>
public class MainScreenService : IMainScreenService
{
    /// <summary>Shown on every tile when nobody is signed in.</summary>
    public const string SignInRequired = "Sign in to use this module.";

    /// <summary>Shown on modules that are not part of this prototype.</summary>
    public const string NotInPrototype = "Not available in this prototype.";

    /// <summary>Shown on the baggage module when the signed-in employee is not baggage staff.</summary>
    public const string NoAccess = "Your role does not have access to this module.";

    /// <inheritdoc />
    public List<ModuleTile> GetModules(SignedInEmployee? employee)
    {
        // 1. Work out whether the person can use the baggage module.
        bool signedIn = employee != null;
        bool isBaggageStaff = signedIn && Roles.IsBaggageRole(employee!.Role);

        List<ModuleTile> tiles = new List<ModuleTile>();

        // 2. Turnaround planning: not built in this prototype.
        ModuleTile turnaround = new ModuleTile();
        turnaround.Name = "Turnaround Planning and Resource Allocation";
        turnaround.Description = "Plan aircraft turnarounds and assign staff and equipment.";
        turnaround.CanOpen = false;
        turnaround.LockedReason = signedIn ? NotInPrototype : SignInRequired;
        tiles.Add(turnaround);

        // 3. Baggage: the module this prototype delivers.
        ModuleTile baggage = new ModuleTile();
        baggage.Name = "Baggage Unloading, Transfer and Loading Management";
        baggage.Description = "Check departure manifests, load bags and manage baggage problems.";
        baggage.PagePath = "/Baggage/Index";
        baggage.CanOpen = isBaggageStaff;
        if (!signedIn)
        {
            baggage.LockedReason = SignInRequired;
        }
        else if (!isBaggageStaff)
        {
            baggage.LockedReason = NoAccess;
        }
        tiles.Add(baggage);

        // 4. Departure readiness: not built in this prototype.
        ModuleTile departure = new ModuleTile();
        departure.Name = "Departure Readiness and Turnaround Completion";
        departure.Description = "Confirm the aircraft is ready to depart and close the turnaround.";
        departure.CanOpen = false;
        departure.LockedReason = signedIn ? NotInPrototype : SignInRequired;
        tiles.Add(departure);

        return tiles;
    }
}
