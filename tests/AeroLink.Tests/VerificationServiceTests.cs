using AeroLink.Web.Data;
using AeroLink.Web.Models;
using AeroLink.Web.Services.SpecialHandling;
using AeroLink.Web.Services.Verification;
using AeroLink.Web.Shared;

namespace AeroLink.Tests;

/// <summary>
/// Unit tests for VerificationService (I2): checking a typed bag tag and confirming the bag as loaded.
/// Each test gets a fresh copy of the seeded database.
/// Seed facts used: flight 101 has bags DEMO-101-001 to 010 and no open exceptions;
/// DEMO-101-009 is a wheelchair; flight 102's bag DEMO-102-001 has an open exception (exception 6);
/// flights 101 and 110 are both flight number DEMO101 on different days.
/// </summary>
public class VerificationServiceTests
{
    private const int EmployeeId = 100101;

    // ---------- Helpers ----------

    /// <summary>Counts the loaded bags on a flight, reading straight from the database.</summary>
    private static int CountLoadedBags(TestDatabase testDb, int flightId)
    {
        using AppDbContext db = testDb.CreateContext();
        return db.Bags.Count(b => b.FlightId == flightId && b.Outcome == BagOutcome.Loaded);
    }

    /// <summary>Changes one bag in the database before a test runs.</summary>
    private static void UpdateBag(TestDatabase testDb, string tag, Action<Bag> change)
    {
        using AppDbContext db = testDb.CreateContext();
        Bag bag = db.Bags.Single(b => b.Tag == tag);
        change(bag);
        db.SaveChanges();
    }

    /// <summary>Changes one flight in the database before a test runs.</summary>
    private static void UpdateFlight(TestDatabase testDb, int flightId, Action<Flight> change)
    {
        using AppDbContext db = testDb.CreateContext();
        Flight flight = db.Flights.Single(f => f.FlightId == flightId);
        change(flight);
        db.SaveChanges();
    }

    // ---------- CheckTagAsync: tag is accepted ----------

    [Fact]
    public async Task Check_ValidTagOnSelectedFlight_IsOkToLoad()
    {
        // Arrange
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        VerificationService service = new VerificationService(db);

        // Act
        VerificationResult result = await service.CheckTagAsync(101, "DEMO-101-001");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(VerificationProblem.None, result.Problem);
        Assert.Equal("DEMO-101-001", result.Tag);
    }

    [Fact]
    public async Task Check_DoesNotSaveAnything()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        VerificationService service = new VerificationService(db);

        await service.CheckTagAsync(101, "DEMO-101-001");

        using AppDbContext check = testDb.CreateContext();
        Assert.Equal(BagOutcome.Pending, check.Bags.Single(b => b.Tag == "DEMO-101-001").Outcome);
        Assert.Equal(LoadingStatus.NotStarted, check.Flights.Single(f => f.FlightId == 101).LoadingStatus);
    }

    [Theory]
    [InlineData("demo-101-001")]        // lower case
    [InlineData("  DEMO-101-001  ")]    // spaces either side
    [InlineData("Demo-101-001\t")]      // mixed case and a tab
    public async Task Check_TagTypedDifferently_IsStillAccepted(string typedTag)
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        VerificationService service = new VerificationService(db);

        VerificationResult result = await service.CheckTagAsync(101, typedTag);

        Assert.True(result.Success);
        Assert.Equal("DEMO-101-001", result.Tag);
    }

    [Fact]
    public async Task Check_SpecialBag_ReturnsTypeAndInstruction()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        VerificationService service = new VerificationService(db);

        VerificationResult result = await service.CheckTagAsync(101, "DEMO-101-009");

        Assert.True(result.Success);
        Assert.True(result.IsSpecial);
        Assert.Equal(HandlingType.Wheelchair, result.HandlingType);
        Assert.Equal("Handle carefully. Keep supplied parts together.", result.HandlingInstruction);
    }

    [Fact]
    public async Task Check_StandardBag_IsNotSpecial()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        VerificationService service = new VerificationService(db);

        VerificationResult result = await service.CheckTagAsync(101, "DEMO-101-001");

        Assert.False(result.IsSpecial);
    }

    [Fact]
    public async Task Check_ExceptionResolvedBySupervisor_BagCanBeLoaded()
    {
        // Arrange: resolve exception 6 on DEMO-102-001.
        using TestDatabase testDb = new TestDatabase();
        using (AppDbContext setup = testDb.CreateContext())
        {
            setup.BaggageExceptions.Single(e => e.ExceptionId == 6).Status = ExceptionStatus.Resolved;
            setup.SaveChanges();
        }

        using AppDbContext db = testDb.CreateContext();
        VerificationService service = new VerificationService(db);

        // Act
        VerificationResult result = await service.CheckTagAsync(102, "DEMO-102-001");

        // Assert
        Assert.True(result.Success);
    }

    // ---------- CheckTagAsync: tag is rejected ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Check_NoTagTyped_IsRejected(string? typedTag)
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        VerificationService service = new VerificationService(db);

        VerificationResult result = await service.CheckTagAsync(101, typedTag);

        Assert.False(result.Success);
        Assert.Equal(VerificationProblem.TagEmpty, result.Problem);
    }

    [Theory]
    [InlineData("DEMO-101-000")]   // one below the first bag
    [InlineData("DEMO-101-011")]   // one above the last bag
    [InlineData("DEMO101001")]     // dashes missing
    [InlineData("ABC")]
    public async Task Check_TagNotInSystem_IsRejected(string typedTag)
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        VerificationService service = new VerificationService(db);

        VerificationResult result = await service.CheckTagAsync(101, typedTag);

        Assert.False(result.Success);
        Assert.Equal(VerificationProblem.TagNotFound, result.Problem);
    }

    [Fact]
    public async Task Check_BagFromAnotherFlight_IsRejected()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        VerificationService service = new VerificationService(db);

        VerificationResult result = await service.CheckTagAsync(101, "DEMO-102-002");

        Assert.False(result.Success);
        Assert.Equal(VerificationProblem.WrongFlight, result.Problem);
        Assert.Contains("DEMO102", result.Message);
    }

    [Fact]
    public async Task Check_BagFromSameFlightNumberOnAnotherDay_IsRejected()
    {
        // Flight 110 is also DEMO101, but departs a day later.
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        VerificationService service = new VerificationService(db);

        VerificationResult result = await service.CheckTagAsync(101, "DEMO-110-001");

        Assert.False(result.Success);
        Assert.Equal(VerificationProblem.WrongFlight, result.Problem);
        Assert.Contains("Tue 6 Oct", result.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(999)]
    public async Task Check_FlightDoesNotExist_IsRejected(int flightId)
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        VerificationService service = new VerificationService(db);

        VerificationResult result = await service.CheckTagAsync(flightId, "DEMO-101-001");

        Assert.False(result.Success);
        Assert.Equal(VerificationProblem.FlightNotFound, result.Problem);
    }

    [Fact]
    public async Task Check_LoadingClosed_IsRejected()
    {
        using TestDatabase testDb = new TestDatabase();
        UpdateFlight(testDb, 101, f => f.LoadingStatus = LoadingStatus.Completed);
        using AppDbContext db = testDb.CreateContext();
        VerificationService service = new VerificationService(db);

        VerificationResult result = await service.CheckTagAsync(101, "DEMO-101-001");

        Assert.False(result.Success);
        Assert.Equal(VerificationProblem.LoadingClosed, result.Problem);
    }

    [Fact]
    public async Task Check_BagAlreadyLoaded_IsRejected()
    {
        using TestDatabase testDb = new TestDatabase();
        UpdateBag(testDb, "DEMO-101-001", b => b.Outcome = BagOutcome.Loaded);
        using AppDbContext db = testDb.CreateContext();
        VerificationService service = new VerificationService(db);

        VerificationResult result = await service.CheckTagAsync(101, "DEMO-101-001");

        Assert.False(result.Success);
        Assert.Equal(VerificationProblem.AlreadyLoaded, result.Problem);
    }

    [Fact]
    public async Task Check_BagApprovedNotToLoad_IsRejected()
    {
        using TestDatabase testDb = new TestDatabase();
        UpdateBag(testDb, "DEMO-101-001", b => b.Outcome = BagOutcome.ApprovedNotToLoad);
        using AppDbContext db = testDb.CreateContext();
        VerificationService service = new VerificationService(db);

        VerificationResult result = await service.CheckTagAsync(101, "DEMO-101-001");

        Assert.False(result.Success);
        Assert.Equal(VerificationProblem.ApprovedNotToLoad, result.Problem);
    }

    [Fact]
    public async Task Check_BagWithOpenException_IsRejected()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        VerificationService service = new VerificationService(db);

        VerificationResult result = await service.CheckTagAsync(102, "DEMO-102-001");

        Assert.False(result.Success);
        Assert.Equal(VerificationProblem.OpenException, result.Problem);
    }

    // ---------- ConfirmLoadedAsync: bag is loaded ----------

    [Fact]
    public async Task Confirm_ValidBag_IsSavedAsLoadedWithEmployeeAndTime()
    {
        // Arrange
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        VerificationService service = new VerificationService(db);
        DateTime before = DateTime.UtcNow;

        // Act
        VerificationResult result = await service.ConfirmLoadedAsync(101, "DEMO-101-001", EmployeeId);

        // Assert: read back with a new context to prove it was saved.
        DateTime after = DateTime.UtcNow;
        using AppDbContext check = testDb.CreateContext();
        Bag bag = check.Bags.Single(b => b.Tag == "DEMO-101-001");

        Assert.True(result.Success);
        Assert.Equal(BagOutcome.Loaded, bag.Outcome);
        Assert.Equal(EmployeeId, bag.LoadedByEmployeeId);
        Assert.NotNull(bag.LoadedAtUtc);
        Assert.InRange(bag.LoadedAtUtc!.Value, before, after);
    }

    [Fact]
    public async Task Confirm_ValidBag_LoadedCountGoesUpByOne()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        VerificationService service = new VerificationService(db);

        await service.ConfirmLoadedAsync(101, "DEMO-101-001", EmployeeId);

        Assert.Equal(1, CountLoadedBags(testDb, 101));
    }

    [Fact]
    public async Task Confirm_FirstBag_StartsLoadingForFlight()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        VerificationService service = new VerificationService(db);

        await service.ConfirmLoadedAsync(101, "DEMO-101-001", EmployeeId);

        using AppDbContext check = testDb.CreateContext();
        Assert.Equal(LoadingStatus.InProgress, check.Flights.Single(f => f.FlightId == 101).LoadingStatus);
    }

    [Fact]
    public async Task Confirm_LowerCaseTag_LoadsTheRightBag()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        VerificationService service = new VerificationService(db);

        VerificationResult result = await service.ConfirmLoadedAsync(101, " demo-101-005 ", EmployeeId);

        using AppDbContext check = testDb.CreateContext();
        Assert.True(result.Success);
        Assert.Equal(BagOutcome.Loaded, check.Bags.Single(b => b.Tag == "DEMO-101-005").Outcome);
    }

    [Fact]
    public async Task Confirm_EveryBagOnFlight_AllLoadedAndNoneOutstanding()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        VerificationService service = new VerificationService(db);
        SpecialHandlingService specialHandling = new SpecialHandlingService(db, service);
        SignedInEmployee handler = new SignedInEmployee(EmployeeId, "S Mitchell", "Baggage Handler");

        for (int i = 1; i <= 10; i++)
        {
            string tag = "DEMO-101-" + i.ToString("000");
            if (i >= 6)
            {
                // I3: bags 006 to 010 are special and need their handling acknowledged before loading.
                await specialHandling.AcknowledgeHandlingAsync(101, tag, handler);
            }
            VerificationResult result = await service.ConfirmLoadedAsync(101, tag, EmployeeId);
            Assert.True(result.Success);
        }

        Assert.Equal(10, CountLoadedBags(testDb, 101));
    }

    [Fact]
    public async Task Confirm_OnlyChangesTheConfirmedBag()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        VerificationService service = new VerificationService(db);

        await service.ConfirmLoadedAsync(101, "DEMO-101-001", EmployeeId);

        using AppDbContext check = testDb.CreateContext();
        Assert.Equal(1, check.Bags.Count(b => b.Outcome == BagOutcome.Loaded));
    }

    // ---------- ConfirmLoadedAsync: bag is NOT loaded ----------

    [Fact]
    public async Task Confirm_SameBagTwice_SecondIsRejectedAndCountStaysAtOne()
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        VerificationService service = new VerificationService(db);

        await service.ConfirmLoadedAsync(101, "DEMO-101-001", EmployeeId);
        VerificationResult second = await service.ConfirmLoadedAsync(101, "DEMO-101-001", EmployeeId);

        Assert.False(second.Success);
        Assert.Equal(VerificationProblem.AlreadyLoaded, second.Problem);
        Assert.Equal(1, CountLoadedBags(testDb, 101));
    }

    [Theory]
    [InlineData(101, "", VerificationProblem.TagEmpty)]
    [InlineData(101, "DEMO-999-001", VerificationProblem.TagNotFound)]
    [InlineData(101, "DEMO-102-002", VerificationProblem.WrongFlight)]
    [InlineData(102, "DEMO-102-001", VerificationProblem.OpenException)]
    [InlineData(999, "DEMO-101-001", VerificationProblem.FlightNotFound)]
    public async Task Confirm_InvalidBag_NothingIsLoaded(int flightId, string tag, VerificationProblem expected)
    {
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        VerificationService service = new VerificationService(db);

        VerificationResult result = await service.ConfirmLoadedAsync(flightId, tag, EmployeeId);

        using AppDbContext check = testDb.CreateContext();
        Assert.False(result.Success);
        Assert.Equal(expected, result.Problem);
        Assert.Equal(0, check.Bags.Count(b => b.Outcome == BagOutcome.Loaded));
        Assert.Equal(0, check.Flights.Count(f => f.LoadingStatus != LoadingStatus.NotStarted));
    }

    [Fact]
    public async Task Confirm_LoadingClosed_NothingIsLoaded()
    {
        using TestDatabase testDb = new TestDatabase();
        UpdateFlight(testDb, 101, f => f.LoadingStatus = LoadingStatus.Completed);
        using AppDbContext db = testDb.CreateContext();
        VerificationService service = new VerificationService(db);

        VerificationResult result = await service.ConfirmLoadedAsync(101, "DEMO-101-001", EmployeeId);

        Assert.False(result.Success);
        Assert.Equal(VerificationProblem.LoadingClosed, result.Problem);
        Assert.Equal(0, CountLoadedBags(testDb, 101));
    }

    [Fact]
    public async Task Confirm_ExceptionReportedAfterCheck_IsRejected()
    {
        // Arrange: the tag passes the check, then a problem is reported before the employee confirms.
        using TestDatabase testDb = new TestDatabase();
        using AppDbContext db = testDb.CreateContext();
        VerificationService service = new VerificationService(db);
        VerificationResult check = await service.CheckTagAsync(101, "DEMO-101-002");

        using (AppDbContext other = testDb.CreateContext())
        {
            other.BaggageExceptions.Add(new BaggageException
            {
                BagId = 2,
                FlightId = 101,
                Category = ExceptionCategory.DamagedBag,
                Description = "Torn handle.",
                Status = ExceptionStatus.Open,
                ReportedAtUtc = DateTime.UtcNow
            });
            other.SaveChanges();
        }

        // Act: confirm with a fresh service, as a new page request would.
        using AppDbContext db2 = testDb.CreateContext();
        VerificationResult confirm = await new VerificationService(db2).ConfirmLoadedAsync(101, "DEMO-101-002", EmployeeId);

        // Assert
        Assert.True(check.Success);
        Assert.False(confirm.Success);
        Assert.Equal(VerificationProblem.OpenException, confirm.Problem);
        Assert.Equal(0, CountLoadedBags(testDb, 101));
    }

    // ---------- CleanTag ----------

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("demo-101-001", "DEMO-101-001")]
    [InlineData("  Demo-101-001 ", "DEMO-101-001")]
    public void CleanTag_TrimsAndMakesCapitals(string? typed, string expected)
    {
        Assert.Equal(expected, VerificationService.CleanTag(typed));
    }
}
