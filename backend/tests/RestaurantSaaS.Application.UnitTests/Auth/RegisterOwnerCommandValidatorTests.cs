using FluentAssertions;
using FluentValidation.TestHelper;
using RestaurantSaaS.Application.Auth;
using Xunit;

namespace RestaurantSaaS.Application.UnitTests.Auth;

public class RegisterOwnerCommandValidatorTests
{
    private readonly RegisterOwnerCommandValidator _validator = new();

    private static RegisterOwnerCommand ValidCommand() => new(
        CompanyName: "Bella Pizza", Email: "owner@bellapizza.demo", Password: "Str0ng!Passw0rd",
        FirstName: "Bella", LastName: "Owner", PackageName: "Professional");

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void InvalidEmail_FailsValidation(string email)
    {
        var command = ValidCommand() with { Email = email };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void ShortPassword_FailsValidation()
    {
        var command = ValidCommand() with { Password = "short" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public void EmptyCompanyName_FailsValidation()
    {
        var command = ValidCommand() with { CompanyName = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.CompanyName);
    }
}
