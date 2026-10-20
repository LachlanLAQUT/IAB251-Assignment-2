using AeroLink.Web.Data;
using AeroLink.Web.Models;
using AeroLink.Web.Services.SpecialHandling;
using AeroLink.Web.Services.Verification;
using AeroLink.Web.Shared;

namespace AeroLink.Tests;

/// <summary>
/// Unit tests for special-handling acknowledgement (I3) and the rule that a special bag cannot be
/// confirmed as loaded until its handling instruction is acknowledged.
/// Each test gets a fresh copy of the seeded database.
/// Seed facts used: on flight 101, bags DEMO-101-001 to 005 are Standard and 006 to 010 are special
/// (006 Fragile, 008 Oversized, 009 Wheelchair, 010 Sporting equipment). No bag starts acknowledged.
/// Bag DEMO-102-006 is special but on flight 102. Bag DEMO-102-010 has an open exception.
/// </summary>
public class SpecialHandlingServiceTests
{
    private const int Flight101 = 101;
    private const string FragileBag = "DEMO-101-006";
    private const string WheelchairBag = "DEMO-101-009";
    private const string StandardBag = "DEMO-101-001";

    private static readonly SignedInEmployee Handler = new SignedInEmployee(100101, "S Mitchell", "Baggage Handler");

    // ---------- Helpers ----------

    /// <summary>Acknowledges a bag's handling on its own context, as a separate page request would.</summary>
    private static async Task<AcknowledgementResult> AcknowledgeAsync(TestDatabase testDb, int flightId, string tag)
    {
        using AppDbContext db = testDb.CreateContext();
        return await new SpecialHandlingService(db, new VerificationService(db))
            .AcknowledgeHandlingAsync(flightId, tag, Handler);
    }

    /// <summary>Tries to confirm a bag as loaded on its own context.</summary>
    private static async Task<VerificationResult> ConfirmAsync(TestDatabase testDb, int flightId, string tag)
    {
        using AppDbContext db = testDb.CreateContext();
        return await new VerificationService(db).ConfirmLoadedAsync(flightId, tag, Handler.EmployeeId);
    }

    /// <summary>Reads one bag straight from the database.</summary>
    private static Bag ReadBag(TestDatabase testDb, string tag)
    {
        using AppDbContext db = testDb.CreateContext();
        return db.Bags.Single(b => b.Tag == tag);
    }

    /// <summary>Changes one bag before a test runs.</summary>
    private static void UpdateBag(TestDatabase testDb, string tag, Action<Bag> change)
    {
        using AppDbContext db = testDb.CreateContext();
        change(db.Bags.Single(b => b.Tag == tag));
        db.SaveChanges();
    }

    // ---------- Acknowledging ----------

    [Fact]
    public async Task Acknowledge_SpecialBag_IsSavedAndReported()
    {
        using TestDatabase testDb = new TestDatabase();

        AcknowledgementResult result = await AcknowledgeAsync(testDb, Flight101, FragileBag);

        Assert.True(result.Success);
        Assert.Equal(AcknowledgementRejection.None, result.Rejection);
        Assert.False(result.WasAlreadyAcknowledged);
        Assert.Contains("Fragile", result.Message);
        Assert.True(ReadBag(testDb, FragileBag).HandlingAcknowledged);
    }

    [Fact]
    public async Task Acknowledge_DoesNotLoadTheBag()
    {
        using TestDatabase testDb = new TestDatabase();

        await AcknowledgeAsync(testDb, Flight101, FragileBag);

        Assert.Equal(BagOutcome.Pending, ReadBag(testDb, FragileBag).Outcome);
    }

    [Fact]
    public async Task Acknowledge_Twice_SucceedsAndSaysItWasAlreadyDone()
    {
        using TestDatabase testDb = new TestDatabase();
        await AcknowledgeAsync(testDb, Flight101, FragileBag);

        AcknowledgementResult second = await AcknowledgeAsync(testDb, Flight101, FragileBag);

        Assert.True(second.Success);
        Assert.True(second.WasAlreadyAcknowledged);
    }

    [Fact]
    public async Task Acknowledge_TagInLowercaseWithSpaces_MatchesTheBag()
    {
        using TestDatabase testDb = new TestDatabase();

        AcknowledgementResult result = await AcknowledgeAsync(testDb, Flight101, "  demo-101-006 ");

        Assert.True(result.Success);
        Assert.Equal(FragileBag, result.Tag);
    }

    [Fact]
    public async Task Acknowledge_StandardBag_Rejected()
    {
        using TestDatabase testDb = new TestDatabase();

        AcknowledgementResult result = await AcknowledgeAsync(testDb, Flight101, StandardBag);

        Assert.False(result.Success);
        Assert.Equal(AcknowledgementRejection.NotSpecialBag, result.Rejection);
        Assert.False(ReadBag(testDb, StandardBag).HandlingAcknowledged);
    }

    [Theory]
    [InlineData("DEMO-999-001", VerificationProblem.TagNotFound)]
    [InlineData("DEMO-102-006", VerificationProblem.WrongFlight)]
    [InlineData("", VerificationProblem.TagEmpty)]
    public async Task Acknowledge_BagThatFailsVerification_RejectedWithTheVerificationReason(
        string tag, VerificationProblem expectedProblem)
    {
        using TestDatabase testDb = new TestDatabase();

        AcknowledgementResult result = await AcknowledgeAsync(testDb, Flight101, tag);

        Assert.Equal(AcknowledgementRejection.BagNotAccepted, result.Rejection);
        Assert.Equal(expectedProblem, result.BagProblem);
    }

    [Fact]
    public async Task Acknowledge_BagWithOpenProblemReport_Rejected()
    {
        using TestDatabase testDb = new TestDatabase();

        AcknowledgementResult result = await AcknowledgeAsync(testDb, 102, "DEMO-102-010");

        Assert.Equal(AcknowledgementRejection.BagNotAccepted, result.Rejection);
        Assert.Equal(VerificationProblem.OpenException, result.BagProblem);
    }

    [Fact]
    public async Task Acknowledge_BagAlreadyLoaded_Rejected()
    {
        using TestDatabase testDb = new TestDatabase();
        UpdateBag(testDb, FragileBag, b => b.Outcome = BagOutcome.Loaded);

        AcknowledgementResult result = await AcknowledgeAsync(testDb, Flight101, FragileBag);

        Assert.Equal(VerificationProblem.AlreadyLoaded, result.BagProblem);
    }

    // ---------- Confirming a special bag as loaded (rule added to I2) ----------

    [Fact]
    public async Task Check_SpecialBag_ShowsWhetherItIsAcknowledged()
    {
        using TestDatabase testDb = new TestDatabase();
        using (AppDbContext db = testDb.CreateContext())
        {
            Assert.False((await new VerificationService(db).CheckTagAsync(Flight101, WheelchairBag)).HandlingAcknowledged);
        }

        await AcknowledgeAsync(testDb, Flight101, WheelchairBag);

        using AppDbContext after = testDb.CreateContext();
        Assert.True((await new VerificationService(after).CheckTagAsync(Flight101, WheelchairBag)).HandlingAcknowledged);
    }

    [Fact]
    public async Task Confirm_SpecialBagNotAcknowledged_RejectedAndNotLoaded()
    {
        using TestDatabase testDb = new TestDatabase();

        VerificationResult result = await ConfirmAsync(testDb, Flight101, WheelchairBag);

        Assert.False(result.Success);
        Assert.Equal(VerificationProblem.HandlingNotAcknowledged, result.Problem);
        Assert.Equal(BagOutcome.Pending, ReadBag(testDb, WheelchairBag).Outcome);
        Assert.Null(ReadBag(testDb, WheelchairBag).LoadedAtUtc);
    }

    [Fact]
    public async Task Confirm_SpecialBagAfterAcknowledging_IsLoaded()
    {
        using TestDatabase testDb = new TestDatabase();
        await AcknowledgeAsync(testDb, Flight101, WheelchairBag);

        VerificationResult result = await ConfirmAsync(testDb, Flight101, WheelchairBag);

        Assert.True(result.Success);
        Assert.Equal(BagOutcome.Loaded, ReadBag(testDb, WheelchairBag).Outcome);
    }

    [Fact]
    public async Task Confirm_StandardBag_DoesNotNeedAcknowledging()
    {
        using TestDatabase testDb = new TestDatabase();

        VerificationResult result = await ConfirmAsync(testDb, Flight101, StandardBag);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task Confirm_AcknowledgingOneBag_DoesNotUnlockAnother()
    {
        using TestDatabase testDb = new TestDatabase();
        await AcknowledgeAsync(testDb, Flight101, FragileBag);

        VerificationResult result = await ConfirmAsync(testDb, Flight101, WheelchairBag);

        Assert.Equal(VerificationProblem.HandlingNotAcknowledged, result.Problem);
    }
}
