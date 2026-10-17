using AeroLink.Web.Shared;

namespace AeroLink.Web.Services.MainScreen;

/// <summary>
/// Decides which modules appear on the AeroLink main screen and which ones the employee can open (T1).
/// </summary>
public interface IMainScreenService
{
    /// <summary>
    /// Gets the three AeroLink modules in display order.
    /// Nothing can be opened until an employee has signed in.
    /// </summary>
    /// <param name="employee">The signed-in employee, or null if nobody is signed in.</param>
    /// <returns>Three tiles, each saying whether it can be opened and, if not, why.</returns>
    List<ModuleTile> GetModules(SignedInEmployee? employee);
}
