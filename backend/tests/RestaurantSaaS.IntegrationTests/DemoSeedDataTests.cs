using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using RestaurantSaaS.Application.Auth;
using RestaurantSaaS.Application.Dashboard;
using RestaurantSaaS.Application.RestaurantManagement;
using RestaurantSaaS.IntegrationTests.Common;
using Xunit;

namespace RestaurantSaaS.IntegrationTests;

/// <summary>Exercises the demo tenant seeder end-to-end (something no other integration test does, since
/// the shared factory boots with SeedDemoData=false for speed) — the only realistic way to verify the
/// seed data actually works, since this sandbox can't run `docker compose up` itself (registry access is
/// blocked) to check it by hand. Kept to a single [Fact]/single host: the seeder is only ever meant to
/// run once against a given database (exactly what happens in real usage — only the `api` container runs
/// it, never `worker`), so a second independent host seeding the same already-seeded database concurrently
/// is not a scenario worth hardening for.</summary>
[Collection(nameof(IntegrationTestCollection))]
public class DemoSeedDataTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task DemoTenant_LoginWorksAndDashboardShowsSeededActivity()
    {
        var client = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["SeedDemoData"] = "true" })))
            .CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginCommand("owner@bellapizza.demo", "Owner!2026", "integration-test-device"));
        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        loginResponse.IsSuccessStatusCode.Should().BeTrue($"login should succeed but got {loginResponse.StatusCode}: {loginBody}");
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResultDto>();
        login.Should().NotBeNull();
        login!.RequiresMfa.Should().BeFalse();
        login.Tokens.Should().NotBeNull();
        login.Tokens!.AccessToken.Should().NotBeNullOrWhiteSpace();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Tokens.AccessToken);

        var locationsResponse = await client.GetAsync("/api/v1/restaurant-management/locations");
        var locationsBody = await locationsResponse.Content.ReadAsStringAsync();
        locationsResponse.IsSuccessStatusCode.Should().BeTrue($"got {locationsResponse.StatusCode}: {locationsBody}");
        var locations = await locationsResponse.Content.ReadFromJsonAsync<List<LocationDto>>();
        var downtown = locations!.Single(l => l.Name == "Downtown");

        var dashboardResponse = await client.GetAsync($"/api/v1/dashboard/locations/{downtown.Id}/summary");
        var dashboardBody = await dashboardResponse.Content.ReadAsStringAsync();
        dashboardResponse.IsSuccessStatusCode.Should().BeTrue($"got {dashboardResponse.StatusCode}: {dashboardBody}");
        var summary = await dashboardResponse.Content.ReadFromJsonAsync<DashboardSummaryDto>();

        summary.Should().NotBeNull();
        summary!.TodayRevenue.Should().BeGreaterThan(0, "10 paid demo orders were seeded for today");
        summary.TodayOrderCount.Should().Be(10);
        summary.Last7DaysRevenue.Should().BeGreaterThan(summary.TodayRevenue, "6 days of DailySalesSummary history were seeded on top of today");
        summary.Kitchen.Queued.Should().Be(1);
        // Status-based counts aren't mutually exclusive with Overdue: an overdue ticket is still
        // InProgress until marked ready, so this counts both the normal in-progress ticket and the
        // deliberately-overdue one.
        summary.Kitchen.InProgress.Should().Be(2);
        summary.Kitchen.Ready.Should().Be(1);
        summary.Kitchen.Overdue.Should().Be(1);
        summary.InventoryAlerts.Should().Contain(a => a.IngredientName == "Parmesan", "Parmesan was seeded below its reorder threshold");
    }
}
