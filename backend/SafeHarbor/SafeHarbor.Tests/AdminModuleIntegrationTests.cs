using System.Net;
using System.Net.Http.Json;

namespace SafeHarbor.Tests;

public sealed class AdminModuleIntegrationTests : IClassFixture<SafeHarborApiFactory>
{
    private readonly SafeHarborApiFactory _factory;

    public AdminModuleIntegrationTests(SafeHarborApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/admin/caseload/residents")]
    [InlineData("/api/admin/process-recordings")]
    [InlineData("/api/admin/visitation-conferences/visits")]
    [InlineData("/api/admin/visitation-conferences/conferences/upcoming")]
    [InlineData("/api/admin/visitation-conferences/conferences/previous")]
    [InlineData("/api/admin/reports-analytics")]
    [InlineData("/api/admin/donors-contributions/donors")]
    public async Task StaffOrAdmin_ReadEndpoints_AreRoleRestricted(string endpoint)
    {
        using var staffClient = CreateAuthenticatedClient("SocialWorker");
        using var adminClient = CreateAuthenticatedClient("Admin");
        using var donorClient = CreateAuthenticatedClient("Donor");

        Assert.Equal(HttpStatusCode.OK, (await staffClient.GetAsync(endpoint)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await adminClient.GetAsync(endpoint)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await donorClient.GetAsync(endpoint)).StatusCode);
    }

    [Fact]
    public async Task ProcessRecording_Writes_AreSocialWorkerOnly()
    {
        var payload = new
        {
            residentCaseId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            summary = "Follow-up note"
        };

        using var socialWorker = CreateAuthenticatedClient("SocialWorker");
        using var admin = CreateAuthenticatedClient("Admin");

        var allowedResponse = await socialWorker.PostAsJsonAsync("/api/admin/process-recordings", payload);
        var forbiddenResponse = await admin.PostAsJsonAsync("/api/admin/process-recordings", payload);

        Assert.Equal(HttpStatusCode.Created, allowedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
    }

    [Fact]
    public async Task CreateEndpoints_ReturnValidationEnvelope_OnBadPayload()
    {
        using var client = CreateAuthenticatedClient("SocialWorker");

        var residentCaseResponse = await client.PostAsJsonAsync("/api/admin/caseload/residents", new { });
        var processResponse = await client.PostAsJsonAsync("/api/admin/process-recordings", new { });
        var donorResponse = await client.PostAsJsonAsync("/api/admin/donors-contributions/donors", new { name = "a", email = "bad" });

        Assert.Equal(HttpStatusCode.BadRequest, residentCaseResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, processResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, donorResponse.StatusCode);

        var envelope = await residentCaseResponse.Content.ReadFromJsonAsync<ApiErrorEnvelopeContract>();
        Assert.NotNull(envelope);
        Assert.Equal("ValidationError", envelope!.ErrorCode);
    }

    private HttpClient CreateAuthenticatedClient(string role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        client.DefaultRequestHeaders.Add("X-Test-Email", "staff@safeharbor.org");
        return client;
    }

    private sealed record ApiErrorEnvelopeContract(string ErrorCode, string Message, string TraceId);
}
