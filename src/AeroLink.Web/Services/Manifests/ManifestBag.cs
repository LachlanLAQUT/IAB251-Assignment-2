using AeroLink.Web.Models;

namespace AeroLink.Web.Services.Manifests;

/// <summary>
/// One bag on a flight's departure manifest, with what the baggage handler needs to see.
/// </summary>
public class ManifestBag
{
    /// <summary>Bag ID from the manifest.</summary>
    public int BagId { get; set; }

    /// <summary>Bag tag, e.g. DEMO-101-001.</summary>
    public string Tag { get; set; } = "";

    /// <summary>Type of bag. Anything other than Standard needs special handling.</summary>
    public HandlingType HandlingType { get; set; }

    /// <summary>Handling instruction from the manifest.</summary>
    public string HandlingInstruction { get; set; } = "";

    /// <summary>Whether the bag is outstanding, loaded or approved not to load.</summary>
    public BagOutcome Outcome { get; set; }

    /// <summary>True if a problem has been reported for this bag and the supervisor has not decided it yet.</summary>
    public bool HasOpenException { get; set; }

    /// <summary>When the bag was loaded (UTC), or null if it has not been loaded.</summary>
    public DateTime? LoadedAtUtc { get; set; }

    /// <summary>True if the bag needs special handling.</summary>
    public bool IsSpecial
    {
        get { return HandlingType != HandlingType.Standard; }
    }

    /// <summary>Bag type as text for the screen, e.g. "Sporting equipment".</summary>
    public string TypeText
    {
        get
        {
            if (HandlingType == HandlingType.SportingEquipment)
            {
                return "Sporting equipment";
            }
            return HandlingType.ToString();
        }
    }

    /// <summary>Loading status as text for the screen.</summary>
    public string StatusText
    {
        get
        {
            if (Outcome == BagOutcome.Loaded)
            {
                return "Loaded";
            }
            if (Outcome == BagOutcome.ApprovedNotToLoad)
            {
                return "Approved not to load";
            }
            return "Outstanding";
        }
    }

    /// <summary>Loaded time in Brisbane time, or "-" if the bag has not been loaded.</summary>
    public string LoadedAtText
    {
        get
        {
            if (LoadedAtUtc == null)
            {
                return "-";
            }
            return LoadedAtUtc.Value.AddHours(10).ToString("HH:mm");
        }
    }
}
