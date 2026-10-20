using AeroLink.Web.Data;
using AeroLink.Web.Models;
using AeroLink.Web.Services.Exceptions;
using AeroLink.Web.Services.Status;
using AeroLink.Web.Services.Verification;
using AeroLink.Web.Shared;

namespace AeroLink.Tests;

/// <summary>
/// Black-box unit tests for ProblemReportService (I4): a Baggage Handler reports a problem with a bag,
/// which is saved as an open exception for the Supervisor.
/// Each test gets a fresh copy of the seeded database.
/// Seed facts used: flight 101 (DEMO101) has bags DEMO-101-001 to DEMO-101-010, all pending, and no exceptions.
/// Six exceptions (IDs 1 to 6) are seeded as Open; exception 6 is on bag DEMO-102-001 (flight 102).
/// </summary>
public class ProblemReportServiceTests
{
    private const int Flight101 = 101;
    private const string BagOnFlight101 = "DEMO-101-003";
    private const string BagOnFlight102 = "DEMO-102-002";
    private const string BagWithOpenException = "DEMO-102-001";
    private const int SeededExceptionOnThatBag = 6;
    private const int SeededExceptionCount = 6;

    private static readonly SignedInEmployee Handler = new SignedInEmployee(100101, "S Mitchell", "Baggage Handler");
    private static readonly SignedInEmployee Supervisor = new SignedInEmployee(200100, "J Cooper", "BG_Supervisor");

    // ---------- Helpers ----------

    /// <summary>Reports a damaged bag on flight 101 with a simple comment, the most common happy path.</summary>
    private static Task<ProblemReportResult> ReportDamagedBagAsync(AppDbContext db, string tag = BagOnFlight101)
    {
        return new ProblemReportService(db).ReportProblemAsync(
            Flight101, tag, ExceptionCategory.DamagedBag, "Handle torn off.", Handler);
    }

    /// <summary>Reads the open exceptions for one bag straight from the database.</summary>
    private static List<BaggageException> ReadOpenExceptionsForBag(TestDatabase testDb, string tag)
    {
        using AppDbContext db = testDb.CreateContext();
        int bagId = db.Bags.Single(b => b.Tag == tag).BagId;
        return db.BaggageExceptions
            .Where(e => e.BagId == bagId && e.Status == ExceptionStatus.Open)
            .ToList();
    }

    /// <summary>Counts every exception in the database, open or decided.</summary>
    private static int CountExceptions(TestDatabase testDb)
    {
        using AppDbContext db = testDb.CreateContext();
        return db.BaggageExceptions.Count();
    }

    /// <summary>Changes one bag before a test runs.</summary>
    private static void UpdateBag(TestDatabase testDb, string tag, Action<Bag> change)
    {
        using AppDbContext db = testDb.CreateContext();
        change(db.Bags.Single(b => b.Tag == tag));
        db.SaveChanges();
    }

    /// <summary>Changes one flight before a test runs.</summary>
    private static void UpdateFlight(TestDatabase testDb, int flightId, Action<Flight> change)
    {
        using AppDbContext db = testDb.CreateContext();
        change(db.Flights.Single(f => f.FlightId == flightId));
        db.SaveChanges();
    }

    // ---------- A valid report ----------

    [Fact]
    public async Task Report_ValidBag_SavesOpenExceptionWithEveryField()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        ProblemReportResult result = await new ProblemReportService(db).ReportProblemAsync(
            Flight101, BagOnFlight101, ExceptionCategory.DamagedBag, "Handle torn off.", Handler);

        Assert.True(result.Success);
        BaggageException saved = Assert.Single(ReadOpenExceptionsForBag(testDb, BagOnFlight101));
        Assert.Equal(Flight101, saved.FlightId);
        Assert.Equal(ExceptionCategory.DamagedBag, saved.Category);
        Assert.Equal("Handle torn off.", saved.Description);
        Assert.Equal(ExceptionStatus.Open, saved.Status);
        Assert.Equal(Handler.EmployeeId, saved.ReportedByEmployeeId);
        Assert.Null(saved.DecisionReason);
        Assert.Null(saved.DecidedAtUtc);
        Assert.Null(saved.DecidedByEmployeeId);
    }

    [Fact]
    public async Task Report_ValidBag_RecordsTheTimeItWasReported()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        DateTime before = DateTime.UtcNow;
        ProblemReportResult result = await ReportDamagedBagAsync(db);
        DateTime after = DateTime.UtcNow;

        BaggageException saved = Assert.Single(ReadOpenExceptionsForBag(testDb, BagOnFlight101));
        Assert.InRange(saved.ReportedAtUtc, before, after);
        Assert.Equal(saved.ReportedAtUtc, result.ReportedAtUtc);
    }

    [Fact]
    public async Task Report_ValidBag_ReturnsNewExceptionIdAndConfirmation()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        ProblemReportResult result = await ReportDamagedBagAsync(db);

        BaggageException saved = Assert.Single(ReadOpenExceptionsForBag(testDb, BagOnFlight101));
        Assert.Equal(ReportRejection.None, result.Rejection);
        Assert.Equal(saved.ExceptionId, result.ExceptionId);
        Assert.True(result.ExceptionId > SeededExceptionCount);
        Assert.Equal(BagOnFlight101, result.Tag);
        Assert.Contains(BagOnFlight101, result.Message);
    }

    [Fact]
    public async Task Report_ValidBag_LeavesBagPendingAndFlightUnchanged()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        await ReportDamagedBagAsync(db);

        using AppDbContext check = testDb.CreateContext();
        Assert.Equal(BagOutcome.Pending, check.Bags.Single(b => b.Tag == BagOnFlight101).Outcome);
        Assert.Equal(LoadingStatus.NotStarted, check.Flights.Single(f => f.FlightId == Flight101).LoadingStatus);
    }

    [Theory]
    [InlineData(ExceptionCategory.MissingBag)]
    [InlineData(ExceptionCategory.DamagedBag)]
    [InlineData(ExceptionCategory.Other)]
    public async Task Report_EachProblemType_IsAccepted(ExceptionCategory category)
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        ProblemReportResult result = await new ProblemReportService(db).ReportProblemAsync(
            Flight101, BagOnFlight101, category, "Problem found during loading.", Handler);

        Assert.True(result.Success);
        Assert.Equal(category, Assert.Single(ReadOpenExceptionsForBag(testDb, BagOnFlight101)).Category);
    }

    [Fact]
    public async Task Report_TagInLowercaseWithSpaces_MatchesTheBag()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        ProblemReportResult result = await ReportDamagedBagAsync(db, "  demo-101-003 ");

        Assert.True(result.Success);
        Assert.Equal(BagOnFlight101, result.Tag);
        Assert.Single(ReadOpenExceptionsForBag(testDb, BagOnFlight101));
    }

    [Fact]
    public async Task Report_CommentWithSurroundingSpaces_IsSavedTrimmed()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        await new ProblemReportService(db).ReportProblemAsync(
            Flight101, BagOnFlight101, ExceptionCategory.MissingBag, "   Not in the loading area.   ", Handler);

        Assert.Equal("Not in the loading area.", Assert.Single(ReadOpenExceptionsForBag(testDb, BagOnFlight101)).Description);
    }

    [Fact]
    public async Task Report_CommentOfExactly500Characters_IsAccepted()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        string longestAllowed = new string('x', ProblemReportService.MaxCommentLength);

        ProblemReportResult result = await new ProblemReportService(db).ReportProblemAsync(
            Flight101, BagOnFlight101, ExceptionCategory.Other, longestAllowed, Handler);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task Report_BagWhoseEarlierProblemWasResolved_CanBeReportedAgain()
    {
        using TestDatabase testDb = new TestDatabase();
        using (AppDbContext supervisorDb = testDb.CreateContext())
        {
            DecisionResult decision = await new ExceptionDecisionService(supervisorDb)
                .ResolveAsync(SeededExceptionOnThatBag, "Bag found.", Supervisor);
            Assert.True(decision.Success);
        }
        using AppDbContext db = testDb.CreateContext();

        ProblemReportResult result = await new ProblemReportService(db).ReportProblemAsync(
            102, BagWithOpenException, ExceptionCategory.DamagedBag, "Now the zip is broken.", Handler);

        Assert.True(result.Success);
    }

    // ---------- Rejected reports ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Report_EmptyTag_Rejected(string? tag)
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        ProblemReportResult result = await ReportDamagedBagAsync(db, tag!);

        Assert.False(result.Success);
        Assert.Equal(ReportRejection.TagEmpty, result.Rejection);
    }

    [Fact]
    public async Task Report_UnknownProblemType_Rejected()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        // A tampered form could post a number that is not one of the three categories.
        ProblemReportResult result = await new ProblemReportService(db).ReportProblemAsync(
            Flight101, BagOnFlight101, (ExceptionCategory)99, "Handle torn off.", Handler);

        Assert.Equal(ReportRejection.CategoryInvalid, result.Rejection);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Report_EmptyComment_Rejected(string? comment)
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        ProblemReportResult result = await new ProblemReportService(db).ReportProblemAsync(
            Flight101, BagOnFlight101, ExceptionCategory.MissingBag, comment, Handler);

        Assert.Equal(ReportRejection.CommentRequired, result.Rejection);
    }

    [Fact]
    public async Task Report_CommentOver500Characters_Rejected()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        string tooLong = new string('x', ProblemReportService.MaxCommentLength + 1);

        ProblemReportResult result = await new ProblemReportService(db).ReportProblemAsync(
            Flight101, BagOnFlight101, ExceptionCategory.Other, tooLong, Handler);

        Assert.Equal(ReportRejection.CommentTooLong, result.Rejection);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(999)]
    public async Task Report_FlightDoesNotExist_Rejected(int flightId)
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        ProblemReportResult result = await new ProblemReportService(db).ReportProblemAsync(
            flightId, BagOnFlight101, ExceptionCategory.MissingBag, "Not in the loading area.", Handler);

        Assert.Equal(ReportRejection.FlightNotFound, result.Rejection);
    }

    [Fact]
    public async Task Report_FlightLoadingClosed_Rejected()
    {
        using TestDatabase testDb = new TestDatabase();
        UpdateFlight(testDb, Flight101, f => f.LoadingStatus = LoadingStatus.Completed);
        using AppDbContext db = testDb.CreateContext();

        ProblemReportResult result = await ReportDamagedBagAsync(db);

        Assert.Equal(ReportRejection.LoadingClosed, result.Rejection);
    }

    [Fact]
    public async Task Report_TagNotInSystem_Rejected()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        ProblemReportResult result = await ReportDamagedBagAsync(db, "DEMO-999-001");

        Assert.Equal(ReportRejection.TagNotFound, result.Rejection);
    }

    [Fact]
    public async Task Report_BagOnAnotherFlight_Rejected()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        ProblemReportResult result = await ReportDamagedBagAsync(db, BagOnFlight102);

        Assert.Equal(ReportRejection.WrongFlight, result.Rejection);
    }

    [Fact]
    public async Task Report_BagAlreadyLoaded_Rejected()
    {
        using TestDatabase testDb = new TestDatabase();
        UpdateBag(testDb, BagOnFlight101, b => b.Outcome = BagOutcome.Loaded);
        using AppDbContext db = testDb.CreateContext();

        ProblemReportResult result = await ReportDamagedBagAsync(db);

        Assert.Equal(ReportRejection.BagAlreadyLoaded, result.Rejection);
    }

    [Fact]
    public async Task Report_BagApprovedNotToLoad_Rejected()
    {
        using TestDatabase testDb = new TestDatabase();
        UpdateBag(testDb, BagOnFlight101, b => b.Outcome = BagOutcome.ApprovedNotToLoad);
        using AppDbContext db = testDb.CreateContext();

        ProblemReportResult result = await ReportDamagedBagAsync(db);

        Assert.Equal(ReportRejection.BagApprovedNotToLoad, result.Rejection);
    }

    [Fact]
    public async Task Report_BagWithSeededOpenException_Rejected()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        ProblemReportResult result = await new ProblemReportService(db).ReportProblemAsync(
            102, BagWithOpenException, ExceptionCategory.MissingBag, "Still missing.", Handler);

        Assert.Equal(ReportRejection.AlreadyReported, result.Rejection);
    }

    [Fact]
    public async Task Report_SameBagTwice_SecondReportRejected()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        await ReportDamagedBagAsync(db);
        ProblemReportResult second = await ReportDamagedBagAsync(db);

        Assert.Equal(ReportRejection.AlreadyReported, second.Rejection);
        Assert.Single(ReadOpenExceptionsForBag(testDb, BagOnFlight101));
    }

    [Fact]
    public async Task Report_Rejected_SavesNothing()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        ProblemReportResult result = await new ProblemReportService(db).ReportProblemAsync(
            Flight101, BagOnFlight101, ExceptionCategory.DamagedBag, "", Handler);

        Assert.False(result.Success);
        Assert.Null(result.ExceptionId);
        Assert.Null(result.ReportedAtUtc);
        Assert.Equal(SeededExceptionCount, CountExceptions(testDb));
    }

    // ---------- Effect on the rest of the loading process ----------

    [Fact]
    public async Task AfterReport_BagCannotBeLoaded()
    {
        using TestDatabase testDb = new TestDatabase();
        using (AppDbContext reportDb = testDb.CreateContext())
        {
            await ReportDamagedBagAsync(reportDb);
        }
        using AppDbContext db = testDb.CreateContext();

        VerificationResult check = await new VerificationService(db).CheckTagAsync(Flight101, BagOnFlight101);

        Assert.False(check.Success);
        Assert.Equal(VerificationProblem.OpenException, check.Problem);
    }

    [Fact]
    public async Task AfterReport_SupervisorSeesItInOpenExceptions()
    {
        using TestDatabase testDb = new TestDatabase();
        using (AppDbContext reportDb = testDb.CreateContext())
        {
            await ReportDamagedBagAsync(reportDb);
        }
        using AppDbContext db = testDb.CreateContext();

        List<ExceptionDetails> open = await new ExceptionDecisionService(db).GetOpenExceptionsAsync(Flight101);

        ExceptionDetails reported = Assert.Single(open);
        Assert.Equal(BagOnFlight101, reported.Tag);
        Assert.Equal("Handle torn off.", reported.Description);
    }

    [Fact]
    public async Task AfterReport_FlightNotReadyToCloseUntilSupervisorDecides()
    {
        using TestDatabase testDb = new TestDatabase();
        // Every bag on flight 101 is loaded except the last one, which is then reported missing.
        for (int bagNumber = 1; bagNumber <= 9; bagNumber++)
        {
            UpdateBag(testDb, "DEMO-101-00" + bagNumber, b => b.Outcome = BagOutcome.Loaded);
        }
        int exceptionId;
        using (AppDbContext reportDb = testDb.CreateContext())
        {
            ProblemReportResult report = await new ProblemReportService(reportDb).ReportProblemAsync(
                Flight101, "DEMO-101-010", ExceptionCategory.MissingBag, "Not in the loading area.", Handler);
            exceptionId = report.ExceptionId!.Value;
        }

        using (AppDbContext statusDb = testDb.CreateContext())
        {
            LoadingStatusReport blocked = (await new LoadingStatusService(statusDb).GetReportAsync(Flight101))!;
            Assert.False(blocked.IsReadyToClose);
            Assert.Equal(1, blocked.Flight.OpenExceptions);
        }

        using (AppDbContext supervisorDb = testDb.CreateContext())
        {
            await new ExceptionDecisionService(supervisorDb)
                .ApproveNotToLoadAsync(exceptionId, "Bag did not arrive from check-in.", Supervisor);
        }

        using AppDbContext db = testDb.CreateContext();
        LoadingStatusReport ready = (await new LoadingStatusService(db).GetReportAsync(Flight101))!;
        Assert.True(ready.IsReadyToClose);
    }
}
