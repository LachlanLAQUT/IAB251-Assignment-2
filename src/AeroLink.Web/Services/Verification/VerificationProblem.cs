namespace AeroLink.Web.Services.Verification;

/// <summary>
/// Why a bag tag could not be accepted for loading (I2). None means the bag is OK to load.
/// </summary>
public enum VerificationProblem
{
    /// <summary>No problem. The bag can be loaded.</summary>
    None,

    /// <summary>No tag was typed in.</summary>
    TagEmpty,

    /// <summary>The selected flight does not exist.</summary>
    FlightNotFound,

    /// <summary>Loading for the selected flight has been closed.</summary>
    LoadingClosed,

    /// <summary>No bag in the system has this tag.</summary>
    TagNotFound,

    /// <summary>The bag is on a different flight's manifest.</summary>
    WrongFlight,

    /// <summary>The bag has already been confirmed as loaded.</summary>
    AlreadyLoaded,

    /// <summary>A supervisor approved this bag not to be loaded.</summary>
    ApprovedNotToLoad,

    /// <summary>The bag has a reported problem the supervisor has not decided yet.</summary>
    OpenException
}
