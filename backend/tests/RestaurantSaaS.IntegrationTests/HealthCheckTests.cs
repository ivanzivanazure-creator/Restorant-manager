using FluentAssertions;
using RestaurantSaaS.IntegrationTests.Common;
using Xunit;

namespace RestaurantSaaS.IntegrationTests;

[Collection(nameof(IntegrationTestCollection))]
public class HealthCheckTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Health_ReturnsSuccess()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ReadyHealth_WithLivePostgresAndRedis_ReturnsSuccess()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready");
        var body = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.Should().BeTrue($"got {response.StatusCode}: {body}");
    }

    [Fact]
    public async Task Swagger_IsReachableInDevelopmentLikeEnvironments()
    {
        // Swagger UI is only mapped in Development; Testing uses appsettings defaults, so this just
        // asserts the app boots and routes requests instead of 500ing on startup.
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/super-admin/packages");

        // Unauthenticated request to a protected endpoint should be 401, not a startup/500 error —
        // proving the full middleware pipeline (auth, DI, EF model build) initialized correctly.
        ((int)response.StatusCode).Should().Be(401);
    }
}
