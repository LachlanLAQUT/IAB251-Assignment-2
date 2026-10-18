using AeroLink.Web.Data;
using AeroLink.Web.Models;
using AeroLink.Web.Services.Exceptions;
using AeroLink.Web.Services.Verification;
using AeroLink.Web.Shared;

namespace AeroLink.Tests;

/// <summary>
/// Unit tests for ExceptionDecisionService (I5): listing open exceptions and recording supervisor decisions.
/// Each test gets a fresh copy of the seeded database.
/// Seed facts used: exceptions 1 to 6 are all Open and reported at the same time.
/// Exception 6 is on bag DEMO-102-001 and exception 1 on DEMO-102-010, both on flight 102 (DEMO102).
/// Exceptions 2 to 5 are on flights 104, 105, 107 and 108. Flight 101 has no exceptions.
/// </summary>
public class ExceptionDecisionServiceTests
{
    private const int ExceptionOnFlight102 = 6;
    private const string TagForException6 = "DEMO-102-001";

    private static readonly SignedInEmployee Supervisor = new SignedInEmployee(200100, "J Cooper", "BG_Supervisor");
    private static readonly SignedInEmployee Handler = new SignedInEmployee(100101, "S Mitchell", "Baggage Handler");

    // ---------- Helpers ----------

    /// <summary>Creates the service on a new context for the test database.</summary>
    private static ExceptionDecisionService CreateService(AppDbContext db)
    {
        return new ExceptionDecisionService(db);
    }

    /// <summary>Reads an exception straight from the database, with its bag and flight.</summary>
    private static BaggageException ReadException(TestDatabase testDb, int exceptionId)
    {
        using AppDbContext db = testDb.CreateContext();
        BaggageException exception = db.BaggageExceptions.Single(e => e.ExceptionId == exceptionId);
        exception.Bag = db.Bags.Single(b => b.BagId == exception.BagId);
        exception.Flight = db.Flights.Single(f => f.FlightId == exception.FlightId);
        return exception;
    }

    /// <summary>Checks that nothing about an exception or its bag was saved.</summary>
    private static void AssertNothingSaved(TestDatabase testDb, int exceptionId)
    {
        BaggageException exception = ReadException(testDb, exceptionId);
        Assert.Equal(ExceptionStatus.Open, exception.Status);
        Assert.Null(exception.DecisionReason);
        Assert.Null(exception.DecidedAtUtc);
        Assert.Null(exception.DecidedByEmployeeId);
        Assert.Equal(BagOutcome.Pending, exception.Bag!.Outcome);
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

    // ---------- GetOpenExceptionsAsync ----------

    [Fact]
    public async Task GetOpen_AllFlights_ReturnsEverySeededException()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        List<ExceptionDetails> open = await CreateService(db).GetOpenExceptionsAsync(null);

        Assert.Equal(6, open.Count);
        Assert.All(open, exception => Assert.True(exception.IsOpen));
    }

    [Fact]
    public async Task GetOpen_SameReportTime_OrderedByExceptionId()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        List<ExceptionDetails> open = await CreateService(db).GetOpenExceptionsAsync(null);

        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, open.Select(e => e.ExceptionId));
    }

    [Theory]
    [InlineData(102, 2)]
    [InlineData(104, 1)]
    [InlineData(101, 0)]
    [InlineData(999, 0)]
    public async Task GetOpen_FilteredByFlight_OnlyThatFlight(int flightId, int expectedCount)
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        List<ExceptionDetails> open = await CreateService(db).GetOpenExceptionsAsync(flightId);

        Assert.Equal(expectedCount, open.Count);
        Assert.All(open, exception => Assert.Equal(flightId, exception.FlightId));
    }

    [Fact]
    public async Task GetOpen_IncludesFlightAndBagDetails()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        List<ExceptionDetails> open = await CreateService(db).GetOpenExceptionsAsync(102);
        ExceptionDetails exception = open.Single(e => e.ExceptionId == ExceptionOnFlight102);

        Assert.Equal(TagForException6, exception.Tag);
        Assert.Equal("DEMO102", exception.FlightNumber);
        Assert.Equal("BNE to MEL", exception.Route);
        Assert.Equal("Missing bag", exception.CategoryText);
    }

    [Fact]
    public async Task GetOpen_DecidedException_IsNoLongerListed()
    {
        using TestDatabase testDb = new TestDatabase();
        using (AppDbContext setup = testDb.CreateContext())
        {
            await CreateService(setup).ResolveAsync(ExceptionOnFlight102, "", Supervisor);
        }

        using AppDbContext db = testDb.CreateContext();
        List<ExceptionDetails> open = await CreateService(db).GetOpenExceptionsAsync(102);

        Assert.Single(open);
        Assert.Equal(1, open[0].ExceptionId);
    }

    // ---------- GetExceptionAsync ----------

    [Fact]
    public async Task GetException_Exists_ReturnsDetails()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        ExceptionDetails? details = await CreateService(db).GetExceptionAsync(ExceptionOnFlight102);

        Assert.NotNull(details);
        Assert.Equal(TagForException6, details!.Tag);
        Assert.Equal("Open", details.StatusText);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(999)]
    public async Task GetException_DoesNotExist_ReturnsNull(int exceptionId)
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        ExceptionDetails? details = await CreateService(db).GetExceptionAsync(exceptionId);

        Assert.Null(details);
    }

    // ---------- ResolveAsync: saved ----------

    [Fact]
    public async Task Resolve_OpenException_SavedAsResolvedWithSupervisorAndTime()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        DateTime before = DateTime.UtcNow;

        DecisionResult result = await CreateService(db).ResolveAsync(ExceptionOnFlight102, "  Bag found at gate 3.  ", Supervisor);

        BaggageException saved = ReadException(testDb, ExceptionOnFlight102);
        Assert.True(result.Success);
        Assert.Equal(DecisionProblem.None, result.Problem);
        Assert.Equal(ExceptionStatus.Resolved, saved.Status);
        Assert.Equal("Bag found at gate 3.", saved.DecisionReason);
        Assert.Equal(Supervisor.EmployeeId, saved.DecidedByEmployeeId);
        Assert.InRange(saved.DecidedAtUtc!.Value, before, DateTime.UtcNow);
    }

    [Fact]
    public async Task Resolve_BagStaysPending()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        await CreateService(db).ResolveAsync(ExceptionOnFlight102, "Found.", Supervisor);

        Assert.Equal(BagOutcome.Pending, ReadException(testDb, ExceptionOnFlight102).Bag!.Outcome);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Resolve_NoNote_IsAllowedAndStoredAsNull(string? note)
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        DecisionResult result = await CreateService(db).ResolveAsync(ExceptionOnFlight102, note, Supervisor);

        Assert.True(result.Success);
        Assert.Null(ReadException(testDb, ExceptionOnFlight102).DecisionReason);
    }

    [Fact]
    public async Task Resolve_NoteExactlyAtLimit_IsSaved()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        string note = new string('a', ExceptionDecisionService.MaxReasonLength);

        DecisionResult result = await CreateService(db).ResolveAsync(ExceptionOnFlight102, note, Supervisor);

        Assert.True(result.Success);
        Assert.Equal(note, ReadException(testDb, ExceptionOnFlight102).DecisionReason);
    }

    [Fact]
    public async Task Resolve_ThenBagCanBeLoaded()
    {
        // Ties I5 to I2: before the decision the bag is rejected, after it the bag loads.
        using TestDatabase testDb = new TestDatabase();
        using (AppDbContext before = testDb.CreateContext())
        {
            VerificationResult blocked = await new VerificationService(before).CheckTagAsync(102, TagForException6);
            Assert.Equal(VerificationProblem.OpenException, blocked.Problem);
        }

        using (AppDbContext decide = testDb.CreateContext())
        {
            await CreateService(decide).ResolveAsync(ExceptionOnFlight102, "Found.", Supervisor);
        }

        using AppDbContext after = testDb.CreateContext();
        VerificationResult loaded = await new VerificationService(after).ConfirmLoadedAsync(102, TagForException6, Handler.EmployeeId);
        Assert.True(loaded.Success);
    }

    // ---------- ResolveAsync: refused ----------

    [Fact]
    public async Task Resolve_ByBaggageHandler_IsRefusedAndNothingSaved()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        DecisionResult result = await CreateService(db).ResolveAsync(ExceptionOnFlight102, "Found.", Handler);

        Assert.False(result.Success);
        Assert.Equal(DecisionProblem.NotSupervisor, result.Problem);
        AssertNothingSaved(testDb, ExceptionOnFlight102);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(999)]
    public async Task Resolve_ExceptionDoesNotExist_IsRefused(int exceptionId)
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        DecisionResult result = await CreateService(db).ResolveAsync(exceptionId, "Found.", Supervisor);

        Assert.False(result.Success);
        Assert.Equal(DecisionProblem.ExceptionNotFound, result.Problem);
    }

    [Fact]
    public async Task Resolve_Twice_SecondIsRefusedAndFirstNoteKept()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        ExceptionDecisionService service = CreateService(db);

        await service.ResolveAsync(ExceptionOnFlight102, "First note.", Supervisor);
        DecisionResult second = await service.ResolveAsync(ExceptionOnFlight102, "Second note.", Supervisor);

        Assert.False(second.Success);
        Assert.Equal(DecisionProblem.AlreadyDecided, second.Problem);
        Assert.Equal("First note.", ReadException(testDb, ExceptionOnFlight102).DecisionReason);
    }

    [Fact]
    public async Task Resolve_NoteOverLimit_IsRefusedAndNothingSaved()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        string note = new string('a', ExceptionDecisionService.MaxReasonLength + 1);

        DecisionResult result = await CreateService(db).ResolveAsync(ExceptionOnFlight102, note, Supervisor);

        Assert.False(result.Success);
        Assert.Equal(DecisionProblem.ReasonTooLong, result.Problem);
        AssertNothingSaved(testDb, ExceptionOnFlight102);
    }

    [Fact]
    public async Task Resolve_FlightLoadingClosed_IsRefused()
    {
        using TestDatabase testDb = new TestDatabase();
        UpdateFlight(testDb, 102, f => f.LoadingStatus = LoadingStatus.Completed);
        using AppDbContext db = testDb.CreateContext();

        DecisionResult result = await CreateService(db).ResolveAsync(ExceptionOnFlight102, "Found.", Supervisor);

        Assert.False(result.Success);
        Assert.Equal(DecisionProblem.LoadingClosed, result.Problem);
        AssertNothingSaved(testDb, ExceptionOnFlight102);
    }

    // ---------- ApproveNotToLoadAsync: saved ----------

    [Fact]
    public async Task Approve_WithReason_ExceptionAndBagBothApprovedNotToLoad()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        DateTime before = DateTime.UtcNow;

        DecisionResult result = await CreateService(db).ApproveNotToLoadAsync(
            ExceptionOnFlight102, " Bag not found before departure. ", Supervisor);

        BaggageException saved = ReadException(testDb, ExceptionOnFlight102);
        Assert.True(result.Success);
        Assert.Equal(ExceptionStatus.ApprovedNotToLoad, saved.Status);
        Assert.Equal("Bag not found before departure.", saved.DecisionReason);
        Assert.Equal(Supervisor.EmployeeId, saved.DecidedByEmployeeId);
        Assert.InRange(saved.DecidedAtUtc!.Value, before, DateTime.UtcNow);
        Assert.Equal(BagOutcome.ApprovedNotToLoad, saved.Bag!.Outcome);
        Assert.True(saved.Bag.IsAccountedFor);
    }

    [Fact]
    public async Task Approve_FlightNotStarted_MovesToInProgress()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        await CreateService(db).ApproveNotToLoadAsync(ExceptionOnFlight102, "Not found.", Supervisor);

        Assert.Equal(LoadingStatus.InProgress, ReadException(testDb, ExceptionOnFlight102).Flight!.LoadingStatus);
    }

    [Fact]
    public async Task Approve_OneOfTwoExceptions_OtherStaysOpen()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        await CreateService(db).ApproveNotToLoadAsync(ExceptionOnFlight102, "Not found.", Supervisor);

        Assert.Equal(ExceptionStatus.Open, ReadException(testDb, 1).Status);
        Assert.Equal(BagOutcome.Pending, ReadException(testDb, 1).Bag!.Outcome);
    }

    [Fact]
    public async Task Approve_ThenBagCannotBeLoaded()
    {
        using TestDatabase testDb = new TestDatabase();
        using (AppDbContext decide = testDb.CreateContext())
        {
            await CreateService(decide).ApproveNotToLoadAsync(ExceptionOnFlight102, "Not found.", Supervisor);
        }

        using AppDbContext db = testDb.CreateContext();
        VerificationResult result = await new VerificationService(db).ConfirmLoadedAsync(102, TagForException6, Handler.EmployeeId);

        Assert.False(result.Success);
        Assert.Equal(VerificationProblem.ApprovedNotToLoad, result.Problem);
    }

    // ---------- ApproveNotToLoadAsync: refused ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Approve_NoReason_IsRefusedAndNothingSaved(string? reason)
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        DecisionResult result = await CreateService(db).ApproveNotToLoadAsync(ExceptionOnFlight102, reason, Supervisor);

        Assert.False(result.Success);
        Assert.Equal(DecisionProblem.ReasonRequired, result.Problem);
        AssertNothingSaved(testDb, ExceptionOnFlight102);
    }

    [Theory]
    [InlineData(500, true)]
    [InlineData(501, false)]
    public async Task Approve_ReasonLengthBoundary(int length, bool expectedSuccess)
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        DecisionResult result = await CreateService(db).ApproveNotToLoadAsync(
            ExceptionOnFlight102, new string('a', length), Supervisor);

        Assert.Equal(expectedSuccess, result.Success);
        if (!expectedSuccess)
        {
            Assert.Equal(DecisionProblem.ReasonTooLong, result.Problem);
            AssertNothingSaved(testDb, ExceptionOnFlight102);
        }
    }

    [Fact]
    public async Task Approve_ByBaggageHandler_IsRefusedAndNothingSaved()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        DecisionResult result = await CreateService(db).ApproveNotToLoadAsync(ExceptionOnFlight102, "Not found.", Handler);

        Assert.False(result.Success);
        Assert.Equal(DecisionProblem.NotSupervisor, result.Problem);
        AssertNothingSaved(testDb, ExceptionOnFlight102);
    }

    [Fact]
    public async Task Approve_BagAlreadyLoaded_IsRefused()
    {
        using TestDatabase testDb = new TestDatabase();
        UpdateBag(testDb, TagForException6, b => b.Outcome = BagOutcome.Loaded);
        using AppDbContext db = testDb.CreateContext();

        DecisionResult result = await CreateService(db).ApproveNotToLoadAsync(ExceptionOnFlight102, "Not found.", Supervisor);

        BaggageException saved = ReadException(testDb, ExceptionOnFlight102);
        Assert.False(result.Success);
        Assert.Equal(DecisionProblem.BagAlreadyLoaded, result.Problem);
        Assert.Equal(ExceptionStatus.Open, saved.Status);
        Assert.Equal(BagOutcome.Loaded, saved.Bag!.Outcome);
    }

    [Fact]
    public async Task Approve_AfterResolve_IsRefused()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        ExceptionDecisionService service = CreateService(db);

        await service.ResolveAsync(ExceptionOnFlight102, "Found.", Supervisor);
        DecisionResult result = await service.ApproveNotToLoadAsync(ExceptionOnFlight102, "Changed my mind.", Supervisor);

        BaggageException saved = ReadException(testDb, ExceptionOnFlight102);
        Assert.False(result.Success);
        Assert.Equal(DecisionProblem.AlreadyDecided, result.Problem);
        Assert.Equal(ExceptionStatus.Resolved, saved.Status);
        Assert.Equal(BagOutcome.Pending, saved.Bag!.Outcome);
    }

    [Fact]
    public async Task Approve_FlightLoadingClosed_IsRefused()
    {
        using TestDatabase testDb = new TestDatabase();
        UpdateFlight(testDb, 102, f => f.LoadingStatus = LoadingStatus.Completed);
        using AppDbContext db = testDb.CreateContext();

        DecisionResult result = await CreateService(db).ApproveNotToLoadAsync(ExceptionOnFlight102, "Not found.", Supervisor);

        Assert.False(result.Success);
        Assert.Equal(DecisionProblem.LoadingClosed, result.Problem);
        AssertNothingSaved(testDb, ExceptionOnFlight102);
    }

    [Fact]
    public async Task Approve_HandlerWithNoReason_ReportsRoleProblemFirst()
    {
        // The role check comes before input checks, so a handler is told they cannot decide at all.
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();

        DecisionResult result = await CreateService(db).ApproveNotToLoadAsync(ExceptionOnFlight102, "", Handler);

        Assert.Equal(DecisionProblem.NotSupervisor, result.Problem);
    }
}
