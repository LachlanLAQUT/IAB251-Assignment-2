using AeroLink.Web.Data;
using AeroLink.Web.Models;
using AeroLink.Web.Services.Closure;
using AeroLink.Web.Services.Exceptions;
using AeroLink.Web.Services.Verification;
using AeroLink.Web.Shared;

namespace AeroLink.Tests;

/// <summary>
/// Unit tests for LoadingClosureService (I6): closing loading only when every bag is accounted for and no
/// exception is open, locking the flight afterwards, and building the final loading summary.
/// Each test gets a fresh copy of the seeded database.
/// Seed facts used: every flight has 10 Pending bags. Flight 101 (DEMO-101-001 to 010) has no exceptions.
/// Flight 102 has two open exceptions: exception 1 on DEMO-102-010 and exception 6 on DEMO-102-001.
/// </summary>
public class LoadingClosureServiceTests
{
    private static readonly SignedInEmployee Supervisor = new SignedInEmployee(200100, "J Cooper", "BG_Supervisor");
    private static readonly SignedInEmployee Handler = new SignedInEmployee(100101, "S Mitchell", "Baggage Handler");

    // ---------- Helpers ----------

    /// <summary>Sets every bag on a flight to Loaded by the handler, straight in the database.</summary>
    private static void LoadWholeFlight(TestDatabase testDb, int flightId)
    {
        using AppDbContext db = testDb.CreateContext();
        foreach (Bag bag in db.Bags.Where(b => b.FlightId == flightId))
        {
            bag.Outcome = BagOutcome.Loaded;
            bag.LoadedAtUtc = DateTime.UtcNow;
            bag.LoadedByEmployeeId = Handler.EmployeeId;
        }
        db.Flights.Single(f => f.FlightId == flightId).LoadingStatus = LoadingStatus.InProgress;
        db.SaveChanges();
    }

    /// <summary>Changes one bag before a test runs.</summary>
    private static void UpdateBag(TestDatabase testDb, string tag, Action<Bag> change)
    {
        using AppDbContext db = testDb.CreateContext();
        change(db.Bags.Single(b => b.Tag == tag));
        db.SaveChanges();
    }

    /// <summary>Reads a flight straight from the database.</summary>
    private static Flight ReadFlight(TestDatabase testDb, int flightId)
    {
        using AppDbContext db = testDb.CreateContext();
        return db.Flights.Single(f => f.FlightId == flightId);
    }

    /// <summary>Tries to close a flight using a fresh context, as a new page request would.</summary>
    private static async Task<ClosureResult> Close(TestDatabase testDb, int flightId, SignedInEmployee employee)
    {
        using AppDbContext db = testDb.CreateContext();
        return await new LoadingClosureService(db).CloseLoadingAsync(flightId, employee);
    }

    /// <summary>Gets the summary for a flight using a fresh context.</summary>
    private static async Task<LoadingSummary?> GetSummary(TestDatabase testDb, int flightId)
    {
        using AppDbContext db = testDb.CreateContext();
        return await new LoadingClosureService(db).GetSummaryAsync(flightId);
    }

    /// <summary>Checks that a flight is still open and has no closing details.</summary>
    private static void AssertStillOpen(TestDatabase testDb, int flightId)
    {
        Flight flight = ReadFlight(testDb, flightId);
        Assert.NotEqual(LoadingStatus.Completed, flight.LoadingStatus);
        Assert.Null(flight.LoadingClosedAtUtc);
        Assert.Null(flight.LoadingClosedByEmployeeId);
    }

    // ---------- CloseLoadingAsync: closed ----------

    [Fact]
    public async Task Close_AllBagsLoaded_ClosedWithSupervisorAndTime()
    {
        using TestDatabase testDb = new TestDatabase();
        LoadWholeFlight(testDb, 101);
        DateTime before = DateTime.UtcNow;

        ClosureResult result = await Close(testDb, 101, Supervisor);

        Flight flight = ReadFlight(testDb, 101);
        Assert.True(result.Success);
        Assert.Equal(ClosureProblem.None, result.Problem);
        Assert.Equal(LoadingStatus.Completed, flight.LoadingStatus);
        Assert.Equal(Supervisor.EmployeeId, flight.LoadingClosedByEmployeeId);
        Assert.InRange(flight.LoadingClosedAtUtc!.Value, before, DateTime.UtcNow);
    }

    [Fact]
    public async Task Close_MixOfLoadedAndApprovedNotToLoad_IsClosed()
    {
        using TestDatabase testDb = new TestDatabase();
        LoadWholeFlight(testDb, 101);
        UpdateBag(testDb, "DEMO-101-004", b => { b.Outcome = BagOutcome.ApprovedNotToLoad; b.LoadedAtUtc = null; });

        ClosureResult result = await Close(testDb, 101, Supervisor);

        Assert.True(result.Success);
        Assert.Equal(LoadingStatus.Completed, ReadFlight(testDb, 101).LoadingStatus);
    }

    [Fact]
    public async Task Close_FullWorkflowOnFlightWithExceptions_IsClosed()
    {
        // Runs the real I2 and I5 services: resolve one exception, approve the other bag not to load,
        // load every other bag, then close.
        using TestDatabase testDb = new TestDatabase();
        using (AppDbContext db = testDb.CreateContext())
        {
            ExceptionDecisionService decisions = new ExceptionDecisionService(db);
            Assert.True((await decisions.ResolveAsync(1, "Found in transfer area.", Supervisor)).Success);
            Assert.True((await decisions.ApproveNotToLoadAsync(6, "Not found before departure.", Supervisor)).Success);
        }

        using (AppDbContext db = testDb.CreateContext())
        {
            VerificationService verification = new VerificationService(db);
            for (int i = 2; i <= 10; i++)
            {
                VerificationResult loaded = await verification.ConfirmLoadedAsync(102, "DEMO-102-" + i.ToString("000"), Handler.EmployeeId);
                Assert.True(loaded.Success);
            }
        }

        ClosureResult result = await Close(testDb, 102, Supervisor);

        Assert.True(result.Success);
        Assert.Equal(LoadingStatus.Completed, ReadFlight(testDb, 102).LoadingStatus);
    }

    // ---------- CloseLoadingAsync: refused ----------

    [Fact]
    public async Task Close_NewFlight_RefusedAndAllBagsListedAsOutstanding()
    {
        using TestDatabase testDb = new TestDatabase();

        ClosureResult result = await Close(testDb, 101, Supervisor);

        Assert.False(result.Success);
        Assert.Equal(ClosureProblem.NotReady, result.Problem);
        Assert.NotNull(result.Report);
        Assert.Equal(10, result.Report!.OutstandingBags.Count);
        Assert.Contains("10 bags are still outstanding.", result.Message);
        AssertStillOpen(testDb, 101);
    }

    [Fact]
    public async Task Close_OneBagLeft_RefusedAndThatBagIdentified()
    {
        using TestDatabase testDb = new TestDatabase();
        LoadWholeFlight(testDb, 101);
        UpdateBag(testDb, "DEMO-101-007", b => b.Outcome = BagOutcome.Pending);

        ClosureResult result = await Close(testDb, 101, Supervisor);

        Assert.Equal(ClosureProblem.NotReady, result.Problem);
        Assert.Equal("DEMO-101-007", result.Report!.OutstandingBags.Single().Tag);
        AssertStillOpen(testDb, 101);
    }

    [Fact]
    public async Task Close_AllBagsLoadedButExceptionsOpen_RefusedAndExceptionsIdentified()
    {
        using TestDatabase testDb = new TestDatabase();
        LoadWholeFlight(testDb, 102);

        ClosureResult result = await Close(testDb, 102, Supervisor);

        Assert.False(result.Success);
        Assert.Equal(ClosureProblem.NotReady, result.Problem);
        Assert.Empty(result.Report!.OutstandingBags);
        Assert.Equal(new[] { "DEMO-102-010", "DEMO-102-001" }, result.Report.OpenExceptions.Select(e => e.Tag));
        Assert.Contains("2 exceptions are still open", result.Message);
        AssertStillOpen(testDb, 102);
    }

    [Fact]
    public async Task Close_ByBaggageHandler_RefusedEvenWhenReady()
    {
        using TestDatabase testDb = new TestDatabase();
        LoadWholeFlight(testDb, 101);

        ClosureResult result = await Close(testDb, 101, Handler);

        Assert.False(result.Success);
        Assert.Equal(ClosureProblem.NotSupervisor, result.Problem);
        AssertStillOpen(testDb, 101);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(999)]
    public async Task Close_FlightDoesNotExist_Refused(int flightId)
    {
        using TestDatabase testDb = new TestDatabase();

        ClosureResult result = await Close(testDb, flightId, Supervisor);

        Assert.False(result.Success);
        Assert.Equal(ClosureProblem.FlightNotFound, result.Problem);
        Assert.Null(result.Report);
    }

    [Fact]
    public async Task Close_Twice_SecondRefusedAndOriginalCloseTimeKept()
    {
        using TestDatabase testDb = new TestDatabase();
        LoadWholeFlight(testDb, 101);
        await Close(testDb, 101, Supervisor);
        DateTime firstClosedAt = ReadFlight(testDb, 101).LoadingClosedAtUtc!.Value;

        ClosureResult second = await Close(testDb, 101, new SignedInEmployee(200200, "Other", "BG_Supervisor"));

        Flight flight = ReadFlight(testDb, 101);
        Assert.False(second.Success);
        Assert.Equal(ClosureProblem.AlreadyClosed, second.Problem);
        Assert.Equal(firstClosedAt, flight.LoadingClosedAtUtc);
        Assert.Equal(Supervisor.EmployeeId, flight.LoadingClosedByEmployeeId);
    }

    // ---------- After closing: the record is locked ----------

    [Fact]
    public async Task AfterClose_BagCannotBeLoaded()
    {
        using TestDatabase testDb = new TestDatabase();
        LoadWholeFlight(testDb, 101);
        UpdateBag(testDb, "DEMO-101-004", b => b.Outcome = BagOutcome.ApprovedNotToLoad);
        await Close(testDb, 101, Supervisor);

        using AppDbContext db = testDb.CreateContext();
        VerificationResult result = await new VerificationService(db).ConfirmLoadedAsync(101, "DEMO-101-004", Handler.EmployeeId);

        Assert.False(result.Success);
        Assert.Equal(VerificationProblem.LoadingClosed, result.Problem);
    }

    [Fact]
    public async Task AfterClose_ExceptionDecisionCannotBeChanged()
    {
        using TestDatabase testDb = new TestDatabase();
        using (AppDbContext db = testDb.CreateContext())
        {
            ExceptionDecisionService decisions = new ExceptionDecisionService(db);
            await decisions.ResolveAsync(1, "Found.", Supervisor);
            await decisions.ResolveAsync(6, "Found.", Supervisor);
        }
        LoadWholeFlight(testDb, 102);
        await Close(testDb, 102, Supervisor);

        using AppDbContext check = testDb.CreateContext();
        DecisionResult result = await new ExceptionDecisionService(check).ApproveNotToLoadAsync(1, "Changed.", Supervisor);

        Assert.False(result.Success);
        Assert.Equal(DecisionProblem.LoadingClosed, result.Problem);
    }

    // ---------- GetSummaryAsync ----------

    [Fact]
    public async Task Summary_OpenFlight_ReturnsNull()
    {
        using TestDatabase testDb = new TestDatabase();

        Assert.Null(await GetSummary(testDb, 101));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(999)]
    public async Task Summary_FlightDoesNotExist_ReturnsNull(int flightId)
    {
        using TestDatabase testDb = new TestDatabase();

        Assert.Null(await GetSummary(testDb, flightId));
    }

    [Fact]
    public async Task Summary_ClosedFlight_HasCountsAndClosingDetails()
    {
        using TestDatabase testDb = new TestDatabase();
        LoadWholeFlight(testDb, 101);
        await Close(testDb, 101, Supervisor);

        LoadingSummary? summary = await GetSummary(testDb, 101);

        Assert.NotNull(summary);
        Assert.True(summary!.Flight.IsClosed);
        Assert.Equal(10, summary.Flight.ExpectedBags);
        Assert.Equal(10, summary.Flight.LoadedBags);
        Assert.Equal(0, summary.Flight.OutstandingBags);
        Assert.Equal(10, summary.LoadedBags.Count);
        Assert.Empty(summary.ApprovedNotToLoadBags);
        Assert.Empty(summary.Exceptions);
        Assert.Equal(Supervisor.EmployeeId, summary.ClosedByEmployeeId);
        Assert.Equal("Employee 200100", summary.ClosedByText);
    }

    [Fact]
    public async Task Summary_LoadedBags_InTagOrderWithWhoLoadedThem()
    {
        using TestDatabase testDb = new TestDatabase();
        LoadWholeFlight(testDb, 101);
        await Close(testDb, 101, Supervisor);

        LoadingSummary? summary = await GetSummary(testDb, 101);

        List<string> tags = summary!.LoadedBags.Select(b => b.Tag).ToList();
        Assert.Equal(tags.OrderBy(t => t).ToList(), tags);
        Assert.All(summary.LoadedBags, bag => Assert.Equal(Handler.EmployeeId, bag.LoadedByEmployeeId));
    }

    [Fact]
    public async Task Summary_AfterFullWorkflow_RecordsReasonsAndDecisions()
    {
        using TestDatabase testDb = new TestDatabase();
        using (AppDbContext db = testDb.CreateContext())
        {
            ExceptionDecisionService decisions = new ExceptionDecisionService(db);
            await decisions.ResolveAsync(1, "Found in transfer area.", Supervisor);
            await decisions.ApproveNotToLoadAsync(6, "Not found before departure.", Supervisor);
        }
        using (AppDbContext db = testDb.CreateContext())
        {
            VerificationService verification = new VerificationService(db);
            for (int i = 2; i <= 10; i++)
            {
                await verification.ConfirmLoadedAsync(102, "DEMO-102-" + i.ToString("000"), Handler.EmployeeId);
            }
        }
        await Close(testDb, 102, Supervisor);

        LoadingSummary? summary = await GetSummary(testDb, 102);

        Assert.NotNull(summary);
        Assert.Equal(9, summary!.LoadedBags.Count);

        SummaryBag approved = summary.ApprovedNotToLoadBags.Single();
        Assert.Equal("DEMO-102-001", approved.Tag);
        Assert.Equal("Not found before departure.", approved.Reason);

        Assert.Equal(2, summary.Exceptions.Count);
        SummaryException resolved = summary.Exceptions.Single(e => e.ExceptionId == 1);
        Assert.Equal("Resolved", resolved.StatusText);
        Assert.Equal("Found in transfer area.", resolved.DecisionReason);
        Assert.Equal("Approved not to load", summary.Exceptions.Single(e => e.ExceptionId == 6).StatusText);
    }

    [Fact]
    public async Task Summary_DoesNotChangeAnyData()
    {
        using TestDatabase testDb = new TestDatabase();
        LoadWholeFlight(testDb, 101);
        await Close(testDb, 101, Supervisor);
        DateTime closedAt = ReadFlight(testDb, 101).LoadingClosedAtUtc!.Value;

        await GetSummary(testDb, 101);
        await GetSummary(testDb, 101);

        Flight flight = ReadFlight(testDb, 101);
        Assert.Equal(LoadingStatus.Completed, flight.LoadingStatus);
        Assert.Equal(closedAt, flight.LoadingClosedAtUtc);
    }
}
