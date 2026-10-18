namespace AeroLink.Web.Services.Exceptions;

/// <summary>
/// Why a supervisor decision on a baggage exception could not be recorded (I5).
/// None means the decision was saved.
/// </summary>
public enum DecisionProblem
{
    /// <summary>No problem. The decision was saved.</summary>
    None,

    /// <summary>The employee is not a Baggage Supervisor, so cannot decide exceptions.</summary>
    NotSupervisor,

    /// <summary>No exception has the given ID.</summary>
    ExceptionNotFound,

    /// <summary>The exception has already been resolved or approved not to load.</summary>
    AlreadyDecided,

    /// <summary>Loading for the exception's flight has been closed, so the record cannot change.</summary>
    LoadingClosed,

    /// <summary>A reason is required to approve a bag not to load, but none was entered.</summary>
    ReasonRequired,

    /// <summary>The entered reason is longer than the 500 characters the database allows.</summary>
    ReasonTooLong,

    /// <summary>The bag has already been loaded, so it cannot be approved not to load.</summary>
    BagAlreadyLoaded
}
