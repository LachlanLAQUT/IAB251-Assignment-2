namespace AeroLink.Web.Services.Closure;

/// <summary>
/// Why loading for a flight could not be closed (I6). None means loading was closed.
/// </summary>
public enum ClosureProblem
{
    /// <summary>No problem. Loading was closed.</summary>
    None,

    /// <summary>The employee is not a Baggage Supervisor, so cannot close loading.</summary>
    NotSupervisor,

    /// <summary>No flight has the given ID.</summary>
    FlightNotFound,

    /// <summary>Loading for the flight has already been closed.</summary>
    AlreadyClosed,

    /// <summary>Some expected bags are not yet loaded or approved not to load, or an exception is still open.</summary>
    NotReady
}
