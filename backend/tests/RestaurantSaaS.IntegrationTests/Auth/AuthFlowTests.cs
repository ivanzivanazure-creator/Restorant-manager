using System.Net.Http.Json;
using FluentAssertions;
using RestaurantSaaS.Application.Auth;
using RestaurantSaaS.IntegrationTests.Common;
using Xunit;

namespace RestaurantSaaS.IntegrationTests.Auth;

[Collection(nameof(IntegrationTestCollection))]
public class AuthFlowTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Register_ThenLogin_IssuesWorkingAccessToken()
    {
        var client = factory.CreateClient();
        var email = $"owner-{Guid.NewGuid():N}@test.demo";

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterOwnerCommand(
            CompanyName: "Test Restaurant Co", Email: email, Password: "Str0ng!Passw0rd",
            FirstName: "Test", LastName: "Owner", PackageName: "Starter"));

        registerResponse.EnsureSuccessStatusCode();
        var registerTokens = await registerResponse.Content.ReadFromJsonAsync<AuthTokensDto>();
        registerTokens.Should().NotBeNull();
        registerTokens!.AccessToken.Should().NotBeNullOrWhiteSpace();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, "Str0ng!Passw0rd", "integration-test-device"));
        loginResponse.EnsureSuccessStatusCode();

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResultDto>();
        loginResult.Should().NotBeNull();
        loginResult!.RequiresMfa.Should().BeFalse();
        loginResult.Tokens.Should().NotBeNull();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var client = factory.CreateClient();
        var email = $"dup-{Guid.NewGuid():N}@test.demo";
        var command = new RegisterOwnerCommand("Dup Co", email, "Str0ng!Passw0rd", "Dup", "Owner", "Starter");

        (await client.PostAsJsonAsync("/api/v1/auth/register", command)).EnsureSuccessStatusCode();
        var second = await client.PostAsJsonAsync("/api/v1/auth/register", command);

        ((int)second.StatusCode).Should().Be(409);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();
        var email = $"wrongpw-{Guid.NewGuid():N}@test.demo";
        await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterOwnerCommand("Wrong PW Co", email, "Str0ng!Passw0rd", "Wrong", "Owner", "Starter"));

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, "totally-wrong-password", "device"));

        ((int)response.StatusCode).Should().Be(401);
    }
}
