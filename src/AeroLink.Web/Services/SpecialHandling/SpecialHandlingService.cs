using AeroLink.Web.Data;
using AeroLink.Web.Models;
using AeroLink.Web.Services.Verification;
using AeroLink.Web.Shared;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Web.Services.SpecialHandling;

/// <summary>
/// Records special-handling acknowledgements (I3). Bag checks are delegated to bag verification (I2),
/// so a bag that could not be loaded cannot be acknowledged either.
/// </summary>
public class SpecialHandlingService : ISpecialHandlingService
{
    private readonly AppDbContext _database;
    private readonly IVerificationService _verificationService;

    /// <summary>Creates the service.</summary>
    /// <param name="database">The application database.</param>
    /// <param name="verificationService">Bag verification (I2), used to check the bag before acknowledging.</param>
    public SpecialHandlingService(AppDbContext database, IVerificationService verificationService)
    {
        _database = database;
        _verificationService = verificationService;
    }

    /// <inheritdoc />
    public async Task<AcknowledgementResult> AcknowledgeHandlingAsync(int flightId, string? tag,
        SignedInEmployee employee)
    {
        // 1. The bag must pass the same checks as loading: right flight, flight open, not already loaded,
        //    no open problem report. I2 already does this, so reuse it rather than repeat the rules.
        VerificationResult check = await _verificationService.CheckTagAsync(flightId, tag);
        if (!check.Success)
        {
            return AcknowledgementResult.Rejected(AcknowledgementRejection.BagNotAccepted, check.Problem,
                check.Message, check.Tag);
        }

        // 2. Only special bags have an instruction to acknowledge.
        if (!check.IsSpecial)
        {
            return AcknowledgementResult.Rejected(AcknowledgementRejection.NotSpecialBag, VerificationProblem.None,
                "Bag " + check.Tag + " is standard baggage, so there is no handling instruction to acknowledge.",
                check.Tag);
        }

        Bag bag = await _database.Bags.FirstAsync(b => b.Tag == check.Tag);
        string category = bag.HandlingType.ToDisplayName();

        // 3. Acknowledging again is harmless: the employee may have refreshed the page.
        if (bag.HandlingAcknowledged)
        {
            return AcknowledgementResult.Acknowledged(bag.Tag, wasAlreadyAcknowledged: true,
                category + " handling for bag " + bag.Tag + " was already acknowledged. It can be confirmed as loaded.");
        }

        bag.HandlingAcknowledged = true;
        await _database.SaveChangesAsync();

        return AcknowledgementResult.Acknowledged(bag.Tag, wasAlreadyAcknowledged: false,
            category + " handling for bag " + bag.Tag + " acknowledged. It can now be confirmed as loaded.");
    }
}
