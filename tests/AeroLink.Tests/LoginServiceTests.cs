using AeroLink.Tests.Fakes;
using AeroLink.Web.Services.Login;
using AeroLink.Web.Shared;

namespace AeroLink.Tests;

/// <summary>
/// Black-box tests for T2 sign-in: empty fields, data types, HR lookup and role checks.
/// Employees mirror the HR system's seed data.
/// </summary>
public class LoginServiceTests
{
    private readonly FakeHrApiClient _hrApi = new FakeHrApiClient()
        .WithEmployee(100101, "s.mitchell@company.com", Roles.BaggageHandler, "Sarah", "Mitchell")
        .WithEmployee(200100, "j.cooper@company.com", Roles.BaggageSupervisor, "James", "Cooper")
        .WithEmployee(500100, "e.johnson@company.com", "Developer", "Emma", "Johnson");

    private LoginService CreateService() => new(_hrApi);

    [Fact]
    public async Task BaggageHandlerWithMatchingDetailsSignsIn()
    {
        var result = await CreateService().AuthenticateAsync("100101", "s.mitchell@company.com");

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(100101, result.Employee!.EmployeeId);
        Assert.Equal("Sarah Mitchell", result.Employee.FullName);
    }

    [Fact]
    public async Task BaggageSupervisorSignsIn()
    {
        var result = await CreateService().AuthenticateAsync("200100", "j.cooper@company.com");

        Assert.True(result.Succeeded);
        Assert.Equal(Roles.BaggageSupervisor, result.Employee!.JobTitle);
    }

    [Fact]
    public async Task SurroundingSpacesAndEmailCaseAreIgnored()
    {
        var result = await CreateService().AuthenticateAsync(" 100101 ", " S.Mitchell@Company.com ");

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyEmployeeIdIsRejected(string? employeeId)
    {
        var result = await CreateService().AuthenticateAsync(employeeId, "s.mitchell@company.com");

        Assert.False(result.Succeeded);
        Assert.Equal(LoginErrors.EmployeeIdRequired, result.ErrorMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyEmailIsRejected(string? email)
    {
        var result = await CreateService().AuthenticateAsync("100101", email);

        Assert.False(result.Succeeded);
        Assert.Equal(LoginErrors.EmailRequired, result.ErrorMessage);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("100101a")]
    [InlineData("-100101")]
    [InlineData("1001.01")]
    [InlineData("0")]
    [InlineData("99999999999")]
    public async Task EmployeeIdThatIsNotAPositiveWholeNumberIsRejected(string employeeId)
    {
        var result = await CreateService().AuthenticateAsync(employeeId, "s.mitchell@company.com");

        Assert.False(result.Succeeded);
        Assert.Equal(LoginErrors.EmployeeIdNotANumber, result.ErrorMessage);
    }

    [Theory]
    [InlineData("s.mitchell")]
    [InlineData("s.mitchell@")]
    [InlineData("@company.com")]
    [InlineData("Sarah <s.mitchell@company.com>")]
    public async Task EmailInAnInvalidFormatIsRejected(string email)
    {
        var result = await CreateService().AuthenticateAsync("100101", email);

        Assert.False(result.Succeeded);
        Assert.Equal(LoginErrors.EmailInvalidFormat, result.ErrorMessage);
    }

    [Fact]
    public async Task InvalidInputIsRejectedBeforeCallingTheHrSystem()
    {
        await CreateService().AuthenticateAsync("abc", "s.mitchell@company.com");

        Assert.Equal(0, _hrApi.LookupCount);
    }

    [Fact]
    public async Task UnknownEmployeeIdIsRejected()
    {
        var result = await CreateService().AuthenticateAsync("999999", "s.mitchell@company.com");

        Assert.False(result.Succeeded);
        Assert.Equal(LoginErrors.CredentialsNotRecognised, result.ErrorMessage);
    }

    [Fact]
    public async Task EmailBelongingToADifferentEmployeeIsRejected()
    {
        var result = await CreateService().AuthenticateAsync("100101", "j.cooper@company.com");

        Assert.False(result.Succeeded);
        Assert.Equal(LoginErrors.CredentialsNotRecognised, result.ErrorMessage);
    }

    [Fact]
    public async Task EmployeeWithoutABaggageRoleIsRejected()
    {
        var result = await CreateService().AuthenticateAsync("500100", "e.johnson@company.com");

        Assert.False(result.Succeeded);
        Assert.Equal(LoginErrors.NoBaggageModuleAccess, result.ErrorMessage);
    }

    [Fact]
    public async Task HrSystemOutageGivesAFriendlyMessage()
    {
        _hrApi.SimulateOutage = true;

        var result = await CreateService().AuthenticateAsync("100101", "s.mitchell@company.com");

        Assert.False(result.Succeeded);
        Assert.Equal(LoginErrors.HrSystemUnavailable, result.ErrorMessage);
    }
}