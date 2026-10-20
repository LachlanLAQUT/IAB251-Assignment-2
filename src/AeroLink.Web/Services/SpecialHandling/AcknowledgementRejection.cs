namespace AeroLink.Web.Services.SpecialHandling;

/// <summary>
/// Why a special-handling acknowledgement could not be recorded (I3). None means it was recorded.
/// </summary>
public enum AcknowledgementRejection
{
    /// <summary>No problem. The handling instruction is acknowledged.</summary>
    None,

    /// <summary>
    /// The bag failed the bag verification checks (I2), e.g. unknown tag, another flight's bag, already loaded,
    /// or loading closed. The reason is in <see cref="AcknowledgementResult.BagProblem"/>.
    /// </summary>
    BagNotAccepted,

    /// <summary>The bag is standard baggage, so it has no handling instruction to acknowledge.</summary>
    NotSpecialBag
}
