namespace AeroLink.Web.Services.Exceptions;

/// <summary>
/// Why a bag problem report could not be recorded (I4). None means the report was recorded.
/// The checks run in this order and stop at the first one that fails.
/// </summary>
public enum ReportRejection
{
    /// <summary>No problem. The report was recorded as an open exception.</summary>
    None,

    /// <summary>No bag tag was entered.</summary>
    TagEmpty,

    /// <summary>The problem type is not Missing bag, Damaged bag or Other.</summary>
    CategoryInvalid,

    /// <summary>No comment describing the problem was entered.</summary>
    CommentRequired,

    /// <summary>The comment is longer than the 500 characters the database allows.</summary>
    CommentTooLong,

    /// <summary>The selected flight does not exist.</summary>
    FlightNotFound,

    /// <summary>Loading for the selected flight has been closed, so its record cannot change.</summary>
    LoadingClosed,

    /// <summary>No bag in the system has this tag.</summary>
    TagNotFound,

    /// <summary>The bag is on a different flight's manifest.</summary>
    WrongFlight,

    /// <summary>The bag has already been loaded, so a problem can no longer be reported against it.</summary>
    BagAlreadyLoaded,

    /// <summary>A supervisor has already approved this bag not to be loaded.</summary>
    BagApprovedNotToLoad,

    /// <summary>The bag already has an open problem report waiting for a supervisor decision.</summary>
    AlreadyReported
}
