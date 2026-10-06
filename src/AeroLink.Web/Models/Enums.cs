namespace AeroLink.Web.Models;

/// <summary>Current loading state of a departure flight.</summary>
public enum LoadingStatus
{
    /// <summary>No bags have been confirmed as loaded.</summary>
    NotStarted,

    /// <summary>At least one bag is loaded, but loading is still open.</summary>
    InProgress,

    /// <summary>The supervisor closed loading, so the flight can no longer be changed.</summary>
    Completed
}

/// <summary>Current outcome for a bag on the departure manifest.</summary>
public enum BagOutcome
{
    /// <summary>The bag is expected but has not been accounted for yet.</summary>
    Pending,

    /// <summary>The bag was checked against the manifest and loaded.</summary>
    Loaded,

    /// <summary>A supervisor approved not loading the bag and recorded a reason.</summary>
    ApprovedNotToLoad
}

/// <summary>Handling category from the manifest. Anything other than <see cref="HandlingType.Standard"/> needs special handling.</summary>
public enum HandlingType
{
    /// <summary>Regular baggage with no special instructions.</summary>
    Standard,

    /// <summary>The item needs careful handling because it is fragile.</summary>
    Fragile,

    /// <summary>The item must go through the oversized baggage area.</summary>
    Oversized,

    /// <summary>A passenger's wheelchair or other mobility aid.</summary>
    Wheelchair,

    /// <summary>Sports gear such as a bike or golf clubs.</summary>
    SportingEquipment
}

/// <summary>Category of problem reported during baggage loading.</summary>
public enum ExceptionCategory
{
    /// <summary>The expected bag could not be found.</summary>
    MissingBag,

    /// <summary>The bag was found but is damaged.</summary>
    DamagedBag,

    /// <summary>A different problem, explained in the report.</summary>
    Other
}

/// <summary>Current state of a baggage exception report.</summary>
public enum ExceptionStatus
{
    /// <summary>Waiting for a supervisor; the bag cannot be loaded and the flight cannot be closed.</summary>
    Open,

    /// <summary>The issue was resolved, so the bag can return to Pending and be loaded.</summary>
    Resolved,

    /// <summary>A supervisor approved not loading the bag and recorded a reason.</summary>
    ApprovedNotToLoad
}
