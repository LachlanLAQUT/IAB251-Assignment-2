using AeroLink.Web.Data;
using AeroLink.Web.Models;
using AeroLink.Web.Services.Status;

namespace AeroLink.Tests;

/// <summary>
/// Unit tests for LoadingStatusService (T4): the loading status report and its readiness rule.
/// Each test gets a fresh copy of the seeded database.
/// Seed facts used: every flight has 10 bags, all Pending; flight 101 (DEMO-101-001 to 010) has no exceptions;
/// flight 102 has two open exceptions, exception 1 on DEMO-102-010 and exception 6 on DEMO-102-001.
/// </summary>
public class LoadingStatusServiceTests
{
    // ---------- Helpers ----------

    /// <summary>Sets the outcome of the given bags on a flight before a test runs.</summary>
    private static void SetOutcome(TestDatabase testDb, BagOutcome outcome, params string[] tags)
    {
        using AppDbContext db = testDb.CreateContext();
        foreach (Bag bag in db.Bags.Where(b => tags.Contains(b.Tag)))
        {
            bag.Outcome = outcome;
        }
        db.SaveChanges();
    }

    /// <summary>Sets every bag on a flight to the same outcome before a test runs.</summary>
    private static void SetOutcomeForWholeFlight(TestDatabase testDb, int flightId, BagOutcome outcome)
    {
        using AppDbContext db = testDb.CreateContext();
        foreach (Bag bag in db.Bags.Where(b => b.FlightId == flightId))
        {
            bag.Outcome = outcome;
        }
        db.SaveChanges();
    }

    /// <summary>Changes the status of one exception before a test runs.</summary>
    private static void SetExceptionStatus(TestDatabase testDb, int exceptionId, ExceptionStatus status)
    {
        using AppDbContext db = testDb.CreateContext();
        db.BaggageExceptions.Single(e => e.ExceptionId == exceptionId).Status = status;
        db.SaveChanges();
    }

    /// <summary>Gets the report for a flight using a fresh context, as a new page request would.</summary>
    private static async Task<LoadingStatusReport?> GetReport(TestDatabase testDb, int flightId)
    {
        using AppDbContext db = testDb.CreateContext();
        LoadingStatusService service = new LoadingStatusService(db);
        return await service.GetReportAsync(flightId);
    }

    // ---------- Counts ----------

    [Fact]
    public async Task Report_NewFlight_AllBagsOutstandingAndNothingLoaded()
    {
        using TestDatabase testDb = new TestDatabase();

        LoadingStatusReport? report = await GetReport(testDb, 101);

        Assert.NotNull(report);
        Assert.Equal(101, report!.Flight.FlightId);
        Assert.Equal(10, report.Flight.ExpectedBags);
        Assert.Equal(0, report.Flight.LoadedBags);
        Assert.Equal(0, report.Flight.ApprovedNotToLoadBags);
        Assert.Equal(10, report.Flight.OutstandingBags);
        Assert.Equal(0, report.Flight.OpenExceptions);
        Assert.Equal(10, report.OutstandingBags.Count);
        Assert.Empty(report.OpenExceptions);
    }

    [Fact]
    public async Task Report_SomeLoadedSomeApproved_CountsEachOutcome()
    {
        using TestDatabase testDb = new TestDatabase();
        SetOutcome(testDb, BagOutcome.Loaded, "DEMO-101-001", "DEMO-101-002", "DEMO-101-003");
        SetOutcome(testDb, BagOutcome.ApprovedNotToLoad, "DEMO-101-004", "DEMO-101-005");

        LoadingStatusReport? report = await GetReport(testDb, 101);

        Assert.Equal(3, report!.Flight.LoadedBags);
        Assert.Equal(2, report.Flight.ApprovedNotToLoadBags);
        Assert.Equal(5, report.Flight.OutstandingBags);
        Assert.Equal(5, report.AccountedForBags);
    }

    [Fact]
    public async Task Report_OutstandingList_OnlyPendingBagsInTagOrder()
    {
        using TestDatabase testDb = new TestDatabase();
        SetOutcome(testDb, BagOutcome.Loaded, "DEMO-101-001", "DEMO-101-003");
        SetOutcome(testDb, BagOutcome.ApprovedNotToLoad, "DEMO-101-002");

        LoadingStatusReport? report = await GetReport(testDb, 101);

        List<string> tags = report!.OutstandingBags.Select(b => b.Tag).ToList();
        Assert.Equal(7, tags.Count);
        Assert.DoesNotContain("DEMO-101-001", tags);
        Assert.DoesNotContain("DEMO-101-002", tags);
        Assert.DoesNotContain("DEMO-101-003", tags);
        Assert.Equal(tags.OrderBy(t => t).ToList(), tags);
    }

    [Fact]
    public async Task Report_OnlyIncludesBagsFromSelectedFlight()
    {
        using TestDatabase testDb = new TestDatabase();

        LoadingStatusReport? report = await GetReport(testDb, 101);

        Assert.All(report!.OutstandingBags, bag => Assert.StartsWith("DEMO-101-", bag.Tag));
    }

    [Fact]
    public async Task Report_PercentagesMatchCounts()
    {
        using TestDatabase testDb = new TestDatabase();
        SetOutcome(testDb, BagOutcome.Loaded, "DEMO-101-001", "DEMO-101-002", "DEMO-101-003");
        SetOutcome(testDb, BagOutcome.ApprovedNotToLoad, "DEMO-101-004");

        LoadingStatusReport? report = await GetReport(testDb, 101);

        Assert.Equal(30, report!.LoadedPercent);
        Assert.Equal(10, report.ApprovedNotToLoadPercent);
    }

    // ---------- Open exceptions ----------

    [Fact]
    public async Task Report_FlightWithOpenExceptions_ListsThemWithTags()
    {
        using TestDatabase testDb = new TestDatabase();

        LoadingStatusReport? report = await GetReport(testDb, 102);

        Assert.Equal(2, report!.Flight.OpenExceptions);
        Assert.Equal(2, report.OpenExceptions.Count);
        Assert.Equal(new[] { "DEMO-102-010", "DEMO-102-001" }, report.OpenExceptions.Select(e => e.Tag));
        Assert.Equal("Missing bag", report.OpenExceptions[0].CategoryText);
    }

    [Theory]
    [InlineData(ExceptionStatus.Resolved)]
    [InlineData(ExceptionStatus.ApprovedNotToLoad)]
    public async Task Report_DecidedException_IsNoLongerListed(ExceptionStatus decision)
    {
        using TestDatabase testDb = new TestDatabase();
        SetExceptionStatus(testDb, 6, decision);

        LoadingStatusReport? report = await GetReport(testDb, 102);

        Assert.Equal(1, report!.Flight.OpenExceptions);
        Assert.Single(report.OpenExceptions);
        Assert.Equal("DEMO-102-010", report.OpenExceptions[0].Tag);
    }

    [Fact]
    public async Task Report_OutstandingBagWithOpenException_IsFlagged()
    {
        using TestDatabase testDb = new TestDatabase();

        LoadingStatusReport? report = await GetReport(testDb, 102);

        Assert.True(report!.OutstandingBags.Single(b => b.Tag == "DEMO-102-001").HasOpenException);
        Assert.False(report.OutstandingBags.Single(b => b.Tag == "DEMO-102-002").HasOpenException);
    }

    // ---------- Readiness ----------

    [Fact]
    public async Task Readiness_NewFlight_NotReadyBecauseBagsOutstanding()
    {
        using TestDatabase testDb = new TestDatabase();

        LoadingStatusReport? report = await GetReport(testDb, 101);

        Assert.False(report!.IsReadyToClose);
        Assert.Equal("Not ready to close", report.ReadinessText);
        Assert.Single(report.BlockingReasons);
        Assert.Equal("10 bags are still outstanding.", report.BlockingReasons[0]);
    }

    [Fact]
    public async Task Readiness_OneBagLeft_NotReadyAndUsesSingularWording()
    {
        using TestDatabase testDb = new TestDatabase();
        SetOutcomeForWholeFlight(testDb, 101, BagOutcome.Loaded);
        SetOutcome(testDb, BagOutcome.Pending, "DEMO-101-010");

        LoadingStatusReport? report = await GetReport(testDb, 101);

        Assert.False(report!.IsReadyToClose);
        Assert.Equal("1 bag is still outstanding.", report.BlockingReasons.Single());
        Assert.Equal("DEMO-101-010", report.OutstandingBags.Single().Tag);
    }

    [Fact]
    public async Task Readiness_AllBagsLoaded_IsReady()
    {
        using TestDatabase testDb = new TestDatabase();
        SetOutcomeForWholeFlight(testDb, 101, BagOutcome.Loaded);

        LoadingStatusReport? report = await GetReport(testDb, 101);

        Assert.True(report!.IsReadyToClose);
        Assert.Equal("Ready to close", report.ReadinessText);
        Assert.Empty(report.BlockingReasons);
        Assert.Empty(report.OutstandingBags);
    }

    [Fact]
    public async Task Readiness_MixOfLoadedAndApprovedNotToLoad_IsReady()
    {
        using TestDatabase testDb = new TestDatabase();
        SetOutcomeForWholeFlight(testDb, 101, BagOutcome.Loaded);
        SetOutcome(testDb, BagOutcome.ApprovedNotToLoad, "DEMO-101-004", "DEMO-101-009");

        LoadingStatusReport? report = await GetReport(testDb, 101);

        Assert.True(report!.IsReadyToClose);
        Assert.Equal(10, report.AccountedForBags);
    }

    [Fact]
    public async Task Readiness_AllBagsAccountedForButExceptionOpen_NotReady()
    {
        using TestDatabase testDb = new TestDatabase();
        SetOutcomeForWholeFlight(testDb, 102, BagOutcome.Loaded);
        SetExceptionStatus(testDb, 1, ExceptionStatus.Resolved);

        LoadingStatusReport? report = await GetReport(testDb, 102);

        Assert.Equal(0, report!.Flight.OutstandingBags);
        Assert.False(report.IsReadyToClose);
        Assert.Equal("1 exception is still open and waiting for a supervisor decision.",
            report.BlockingReasons.Single());
    }

    [Fact]
    public async Task Readiness_BagsOutstandingAndExceptionsOpen_GivesBothReasons()
    {
        using TestDatabase testDb = new TestDatabase();

        LoadingStatusReport? report = await GetReport(testDb, 102);

        Assert.False(report!.IsReadyToClose);
        Assert.Equal(2, report.BlockingReasons.Count);
        Assert.Contains("10 bags are still outstanding.", report.BlockingReasons);
        Assert.Contains("2 exceptions are still open and waiting for a supervisor decision.", report.BlockingReasons);
    }

    [Fact]
    public async Task Readiness_ClosedFlight_ShowsClosedAndNotReady()
    {
        using TestDatabase testDb = new TestDatabase();
        SetOutcomeForWholeFlight(testDb, 101, BagOutcome.Loaded);
        using (AppDbContext setup = testDb.CreateContext())
        {
            setup.Flights.Single(f => f.FlightId == 101).LoadingStatus = LoadingStatus.Completed;
            setup.SaveChanges();
        }

        LoadingStatusReport? report = await GetReport(testDb, 101);

        Assert.True(report!.Flight.IsClosed);
        Assert.False(report.IsReadyToClose);
        Assert.Equal("Closed", report.ReadinessText);
        Assert.Empty(report.BlockingReasons);
    }

    // ---------- Flight not found and read-only behaviour ----------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(999)]
    public async Task Report_FlightDoesNotExist_ReturnsNull(int flightId)
    {
        using TestDatabase testDb = new TestDatabase();

        LoadingStatusReport? report = await GetReport(testDb, flightId);

        Assert.Null(report);
    }

    [Fact]
    public async Task Report_DoesNotChangeAnyData()
    {
        using TestDatabase testDb = new TestDatabase();

        await GetReport(testDb, 102);

        using AppDbContext check = testDb.CreateContext();
        Assert.All(check.Bags.Where(b => b.FlightId == 102), bag => Assert.Equal(BagOutcome.Pending, bag.Outcome));
        Assert.Equal(2, check.BaggageExceptions.Count(e => e.FlightId == 102 && e.Status == ExceptionStatus.Open));
        Assert.Equal(LoadingStatus.NotStarted, check.Flights.Single(f => f.FlightId == 102).LoadingStatus);
    }

    [Fact]
    public async Task Report_IsStampedWithCurrentTime()
    {
        using TestDatabase testDb = new TestDatabase();
        DateTime before = DateTime.UtcNow;

        LoadingStatusReport? report = await GetReport(testDb, 101);

        Assert.InRange(report!.GeneratedAtUtc, before, DateTime.UtcNow);
    }
}
