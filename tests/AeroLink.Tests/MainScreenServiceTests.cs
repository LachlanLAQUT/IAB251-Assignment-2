using AeroLink.Web.Services.MainScreen;
using AeroLink.Web.Shared;

namespace AeroLink.Tests;

/// <summary>
/// Unit tests for MainScreenService (T1): which modules are shown on the main screen and which can be opened.
/// </summary>
public class MainScreenServiceTests
{
    private static readonly SignedInEmployee Handler = new SignedInEmployee(100101, "Sarah Mitchell", "Baggage Handler");
    private static readonly SignedInEmployee Supervisor = new SignedInEmployee(200100, "James Cooper", "BG_Supervisor");
    private static readonly SignedInEmployee Developer = new SignedInEmployee(500100, "Emma Johnson", "Developer");

    // ---------- Every visitor sees the same three modules ----------

    [Fact]
    public void GetModules_ShowsThreeModulesInOrder()
    {
        // Arrange
        MainScreenService service = new MainScreenService();

        // Act
        List<ModuleTile> tiles = service.GetModules(null);

        // Assert
        Assert.Equal(3, tiles.Count);
        Assert.Equal("Turnaround Planning and Resource Allocation", tiles[0].Name);
        Assert.Equal("Baggage Unloading, Transfer and Loading Management", tiles[1].Name);
        Assert.Equal("Departure Readiness and Turnaround Completion", tiles[2].Name);
    }

    // ---------- Not signed in: nothing can be opened ----------

    [Fact]
    public void GetModules_NotSignedIn_NothingCanBeOpened()
    {
        MainScreenService service = new MainScreenService();

        List<ModuleTile> tiles = service.GetModules(null);

        foreach (ModuleTile tile in tiles)
        {
            Assert.False(tile.CanOpen);
            Assert.Equal(MainScreenService.SignInRequired, tile.LockedReason);
        }
    }

    // ---------- Signed in as baggage staff ----------

    [Fact]
    public void GetModules_BaggageHandler_CanOpenBaggageModule()
    {
        MainScreenService service = new MainScreenService();

        List<ModuleTile> tiles = service.GetModules(Handler);

        Assert.True(tiles[1].CanOpen);
        Assert.Equal("/Baggage/Index", tiles[1].PagePath);
        Assert.Equal("", tiles[1].LockedReason);
    }

    [Fact]
    public void GetModules_Supervisor_CanOpenBaggageModule()
    {
        MainScreenService service = new MainScreenService();

        List<ModuleTile> tiles = service.GetModules(Supervisor);

        Assert.True(tiles[1].CanOpen);
    }

    [Theory]
    [InlineData("Baggage Handler")]
    [InlineData("BG_Supervisor")]
    public void GetModules_SignedIn_OtherModulesAreNotInPrototype(string role)
    {
        MainScreenService service = new MainScreenService();
        SignedInEmployee employee = new SignedInEmployee(1, "Test", role);

        List<ModuleTile> tiles = service.GetModules(employee);

        Assert.False(tiles[0].CanOpen);
        Assert.Equal(MainScreenService.NotInPrototype, tiles[0].LockedReason);
        Assert.False(tiles[2].CanOpen);
        Assert.Equal(MainScreenService.NotInPrototype, tiles[2].LockedReason);
    }

    [Theory]
    [InlineData("baggage handler")]   // HR may return a different letter case
    [InlineData("BG_SUPERVISOR")]
    public void GetModules_RoleInDifferentCase_StillCanOpenBaggage(string role)
    {
        MainScreenService service = new MainScreenService();
        SignedInEmployee employee = new SignedInEmployee(1, "Test", role);

        List<ModuleTile> tiles = service.GetModules(employee);

        Assert.True(tiles[1].CanOpen);
    }

    // ---------- Signed in, but not baggage staff ----------

    [Theory]
    [InlineData("Developer")]
    [InlineData("")]
    [InlineData("Baggage")]   // close to a real role, but not one
    public void GetModules_NotBaggageStaff_CannotOpenBaggage(string role)
    {
        MainScreenService service = new MainScreenService();
        SignedInEmployee employee = new SignedInEmployee(1, "Test", role);

        List<ModuleTile> tiles = service.GetModules(employee);

        Assert.False(tiles[1].CanOpen);
        Assert.Equal(MainScreenService.NoAccess, tiles[1].LockedReason);
    }

    [Fact]
    public void GetModules_AnyoneSignedIn_NoModuleSaysSignIn()
    {
        MainScreenService service = new MainScreenService();

        List<ModuleTile> tiles = service.GetModules(Developer);

        foreach (ModuleTile tile in tiles)
        {
            Assert.NotEqual(MainScreenService.SignInRequired, tile.LockedReason);
        }
    }

    [Fact]
    public void GetModules_OnlyBaggageModuleHasAPage()
    {
        MainScreenService service = new MainScreenService();

        List<ModuleTile> tiles = service.GetModules(Handler);

        Assert.Equal("", tiles[0].PagePath);
        Assert.NotEqual("", tiles[1].PagePath);
        Assert.Equal("", tiles[2].PagePath);
    }
}
