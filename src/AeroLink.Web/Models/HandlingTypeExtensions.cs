namespace AeroLink.Web.Models;

/// <summary>
/// Screen text for <see cref="HandlingType"/> values, so every page names special-handling
/// categories the same way (I3).
/// </summary>
public static class HandlingTypeExtensions
{
    /// <summary>Gets the handling category as text for the screen.</summary>
    /// <param name="handlingType">The bag's handling type.</param>
    /// <returns>A readable name, e.g. "Fragile" or "Sporting equipment".</returns>
    public static string ToDisplayName(this HandlingType handlingType)
    {
        switch (handlingType)
        {
            case HandlingType.SportingEquipment:
                return "Sporting equipment";
            default:
                // The other names (Standard, Fragile, Oversized, Wheelchair) already read well.
                return handlingType.ToString();
        }
    }
}
