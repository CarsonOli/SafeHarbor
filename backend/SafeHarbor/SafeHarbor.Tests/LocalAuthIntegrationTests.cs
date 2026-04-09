using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

namespace SafeHarbor.Tests;

public sealed class LocalAuthIntegrationTests : IClassFixture<SafeHarborApiFactory>
{
    private readonly SafeHarborApiFactory _factory;

    public LocalAuthIntegrationTests(SafeHarborApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_DonorUserToken_ContainsDonorRoleAcrossSupportedClaimTypes()
    {
        using var client = _factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { email = "roleclaims@example.com", role = "Donor", password = "Password123!Aa" });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "roleclaims@example.com", password = "Password123!Aa" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<LoginEnvelope>();
        Assert.NotNull(payload);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(payload!.IdToken);
        var roleValues = token.Claims.Where(c =>
                c.Type == ClaimTypes.Role
                || c.Type == "role"
                || c.Type == "roles")
            .Select(c => c.Value)
            .ToArray();

        // Keep donor claim assertions explicit so future token-shape refactors do not
        // accidentally break donor-only policy checks in either API or frontend code.
        Assert.Contains("Donor", roleValues);
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Donor");
        Assert.Contains(token.Claims, c => c.Type == "role" && c.Value == "Donor");
        Assert.Contains(token.Claims, c => c.Type == "roles" && c.Value == "Donor");
    }

    [Fact]
    public async Task Register_WeakPassword_ReturnsValidationError()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { email = "weakpass@example.com", role = "Donor", password = "weak" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        Assert.NotNull(payload);
        Assert.Equal("ValidationError", payload!.ErrorCode);
        Assert.Contains("Password", payload.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_LockoutThresholdReached_BlocksSubsequentValidPassword()
    {
        using var client = _factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { email = "lockout@example.com", role = "Donor", password = "Password123!Aa" });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        // Identity lockout threshold is configured to 3 failed attempts.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var failedResponse = await client.PostAsJsonAsync(
                "/api/auth/login",
                new { email = "lockout@example.com", password = "WrongPassword123!Aa" });
            Assert.Equal(HttpStatusCode.BadRequest, failedResponse.StatusCode);
        }

        var lockedResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "lockout@example.com", password = "Password123!Aa" });

        Assert.Equal(HttpStatusCode.BadRequest, lockedResponse.StatusCode);
        var lockedPayload = await lockedResponse.Content.ReadFromJsonAsync<ErrorEnvelope>();
        Assert.NotNull(lockedPayload);
        Assert.Equal("InvalidCredentials", lockedPayload!.ErrorCode);
        Assert.Contains("Invalid credentials", lockedPayload.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Me_RequiresAuthentication_AndReturnsCallerProfile()
    {
        using var client = _factory.CreateClient();

        var unauthorized = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        await client.PostAsJsonAsync(
            "/api/auth/register",
            new { email = "me@example.com", role = "Donor", password = "Password123!Aa" });

        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Donor");
        client.DefaultRequestHeaders.Add("X-Test-Email", "me@example.com");

        var meResponse = await client.GetAsync("/api/auth/me");
        meResponse.EnsureSuccessStatusCode();

        var mePayload = await meResponse.Content.ReadFromJsonAsync<MeEnvelope>();
        Assert.NotNull(mePayload);
        Assert.Equal("me@example.com", mePayload!.Email);
        Assert.Contains("Donor", mePayload.Roles);
    }

    private sealed record LoginEnvelope(string IdToken);
    private sealed record ErrorEnvelope(string ErrorCode, string Message, string TraceId);
    private sealed record MeEnvelope(string Email, string[] Roles);
}
