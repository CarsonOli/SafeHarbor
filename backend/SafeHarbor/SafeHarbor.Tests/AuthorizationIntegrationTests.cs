using System.Net;
using System.Net.Http.Json;

namespace SafeHarbor.Tests;

public sealed class AuthorizationIntegrationTests : IClassFixture<SafeHarborApiFactory>
{
    private readonly SafeHarborApiFactory _factory;

    public AuthorizationIntegrationTests(SafeHarborApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/admin/dashboard")]
    [InlineData("/api/donor/dashboard")]
    public async Task ProtectedEndpoints_DenyUnauthenticatedRequests(string endpoint)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(endpoint);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoints_AllowStaffOrAdmin_AndBlockDonor()
    {
        using var staffClient = CreateAuthenticatedClient(role: "SocialWorker", email: "staff@safeharbor.org");
        using var adminClient = CreateAuthenticatedClient(role: "Admin", email: "admin@safeharbor.org");
        using var donorClient = CreateAuthenticatedClient(role: "Donor", email: "alice@example.com");

        var staffResponse = await staffClient.GetAsync("/api/admin/dashboard");
        var adminResponse = await adminClient.GetAsync("/api/admin/dashboard");
        var donorResponse = await donorClient.GetAsync("/api/admin/dashboard");

        Assert.Equal(HttpStatusCode.OK, staffResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, donorResponse.StatusCode);
    }

    [Theory]
    [InlineData("/api/donor/dashboard", "GET")]
    [InlineData("/api/donor/contribution", "POST")]
    public async Task DonorPersonalEndpoints_AllowDonor_AndForbidAdminOrSocialWorker(string endpoint, string method)
    {
        using var donorClient = CreateAuthenticatedClient(role: "Donor", email: "alice@example.com");
        using var adminClient = CreateAuthenticatedClient(role: "Admin", email: "admin@safeharbor.org");
        using var staffClient = CreateAuthenticatedClient(role: "SocialWorker", email: "alice@example.com");

        var donorResponse = await SendDonorRequestAsync(donorClient, endpoint, method);
        var adminResponse = await SendDonorRequestAsync(adminClient, endpoint, method);
        var staffResponse = await SendDonorRequestAsync(staffClient, endpoint, method);

        Assert.Equal(HttpStatusCode.OK, donorResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, adminResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, staffResponse.StatusCode);
    }

    [Fact]
    public async Task DonorEndpoints_BlockCrossUserAccess_WhenCallerClaimsDoNotMatchTargetRecord()
    {
        using var aliceClient = CreateAuthenticatedClient(role: "Donor", email: "alice@example.com");
        var aliceResponse = await aliceClient.GetAsync("/api/donor/dashboard?email=bob@example.com");
        aliceResponse.EnsureSuccessStatusCode();

        var dashboardPayload = await aliceResponse.Content.ReadFromJsonAsync<DonorDashboardEnvelope>();
        Assert.NotNull(dashboardPayload);
        // Query parameter input is intentionally ignored; endpoint must remain scoped to caller claims.
        Assert.Equal("Alice Nguyen", dashboardPayload!.DonorName);

        using var unknownDonorClient = CreateAuthenticatedClient(
            role: "Donor",
            email: "ghost@example.com",
            oid: Guid.NewGuid().ToString());

        var unknownDonorResponse = await unknownDonorClient.GetAsync("/api/donor/dashboard");

        Assert.Equal(HttpStatusCode.NotFound, unknownDonorResponse.StatusCode);
    }

    private HttpClient CreateAuthenticatedClient(string role, string email, string? oid = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        client.DefaultRequestHeaders.Add("X-Test-Email", email);

        if (!string.IsNullOrWhiteSpace(oid))
        {
            client.DefaultRequestHeaders.Add("X-Test-Oid", oid);
        }

        return client;
    }

    private static Task<HttpResponseMessage> SendDonorRequestAsync(HttpClient client, string endpoint, string method)
    {
        // Keep donor endpoint authorization tests focused on policy behavior:
        // POST sends the minimum valid payload so non-donor callers fail at authorization
        // rather than downstream request validation.
        if (string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase))
        {
            return client.PostAsJsonAsync(endpoint, new { amount = 50m });
        }

        return client.GetAsync(endpoint);
    }

    private sealed record DonorDashboardEnvelope(string DonorName);
}
