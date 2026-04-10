using System.Net;
using System.Text.Json;

namespace SafeHarbor.Tests;

public sealed class AdminDashboardIntegrationTests : IClassFixture<SafeHarborApiFactory>
{
    private readonly SafeHarborApiFactory _factory;

    public AdminDashboardIntegrationTests(SafeHarborApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DashboardEndpoint_ReturnsOperationalSummaryPayload()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        client.DefaultRequestHeaders.Add("X-Test-Email", "admin@safeharbor.org");

        var response = await client.GetAsync("/api/admin/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("activeResidents", out var activeResidents));
        Assert.True(activeResidents.GetInt32() >= 0);

        Assert.True(root.TryGetProperty("recentContributions", out var contributions));
        Assert.Equal(JsonValueKind.Array, contributions.ValueKind);

        Assert.True(root.TryGetProperty("upcomingConferences", out var conferences));
        Assert.Equal(JsonValueKind.Array, conferences.ValueKind);

        Assert.True(root.TryGetProperty("summaryOutcomes", out var outcomes));
        Assert.Equal(JsonValueKind.Array, outcomes.ValueKind);
        Assert.True(outcomes.GetArrayLength() > 0);
    }
}
