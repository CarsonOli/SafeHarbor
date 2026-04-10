using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SafeHarbor.Data;
using SafeHarbor.Models.Entities;

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
            new { email = "roleclaims@example.com", firstName = "Role", lastName = "Claims", password = "Password123!Aa" });
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
    public async Task Login_DatabaseStaffRole_MapsToSocialWorkerClaimsAndKeepsDbRoleClaim()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SafeHarborDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = "staffclaims@example.com",
                Role = "staff",
                FirstName = "Staff",
                LastName = "Claims",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            user.PasswordHash = passwordHasher.HashPassword(user, "Password123!Aa");
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "staffclaims@example.com", password = "Password123!Aa" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<LoginEnvelope>();
        Assert.NotNull(payload);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(payload!.IdToken);
        Assert.Contains(token.Claims, c => c.Type == "db_role" && c.Value == "staff");
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "SocialWorker");
        Assert.Contains(token.Claims, c => c.Type == "role" && c.Value == "SocialWorker");
        Assert.Contains(token.Claims, c => c.Type == "roles" && c.Value == "SocialWorker");
    }

    [Fact]
    public async Task Register_WeakPassword_ReturnsValidationError()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { email = "weakpass@example.com", firstName = "Weak", lastName = "Pass", password = "weak" });

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
            new { email = "lockout@example.com", firstName = "Lock", lastName = "Out", password = "Password123!Aa" });
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
            new { email = "me@example.com", firstName = "Me", lastName = "Example", password = "Password123!Aa" });

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

    [Fact]
    public async Task Register_Donor_CanImmediatelyAccessDonorDashboard_WithoutManualSeeding()
    {
        using var client = _factory.CreateClient();
        var donorEmail = "newly-registered-donor@example.com";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { email = donorEmail, firstName = "Newly", lastName = "Registered", password = "Password123!Aa" });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Donor");
        client.DefaultRequestHeaders.Add("X-Test-Email", donorEmail);

        // Regression test: donor domain profile should be auto-provisioned during registration,
        // so dashboard works immediately with only auth identity claims.
        var dashboardResponse = await client.GetAsync("/api/donor/dashboard");
        Assert.Equal(HttpStatusCode.OK, dashboardResponse.StatusCode);
    }

    [Fact]
    public async Task Register_IgnoresClientRoleAndPersistsUserRole()
    {
        using var client = _factory.CreateClient();
        const string email = "self-role-attempt@example.com";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { email, firstName = "Self", lastName = "Role", password = "Password123!Aa", role = "Admin" });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SafeHarborDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == email);

        // SECURITY: self-service registration must never create elevated roles.
        Assert.Equal("user", user.Role);
    }

    [Fact]
    public async Task ReconcileDomainProfiles_BackfillsExistingLighthouseUsersDonorProfiles()
    {
        const string donorEmail = "legacy-donor@example.com";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SafeHarborDbContext>();
            db.Users.Add(new User
            {
                UserId = Guid.NewGuid(),
                Email = donorEmail,
                Role = "user",
                PasswordHash = "not-used-in-this-test",
                FirstName = "Legacy",
                LastName = "Donor",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        using var donorClient = _factory.CreateClient();
        donorClient.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        donorClient.DefaultRequestHeaders.Add("X-Test-Role", "Donor");
        donorClient.DefaultRequestHeaders.Add("X-Test-Email", donorEmail);

        var beforeReconcile = await donorClient.GetAsync("/api/donor/dashboard");
        Assert.Equal(HttpStatusCode.NotFound, beforeReconcile.StatusCode);

        using var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        adminClient.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        adminClient.DefaultRequestHeaders.Add("X-Test-Email", "admin@safeharbor.org");

        var reconcileResponse = await adminClient.PostAsync("/api/admin/auth-maintenance/reconcile-domain-profiles", null);
        Assert.Equal(HttpStatusCode.OK, reconcileResponse.StatusCode);

        var afterReconcile = await donorClient.GetAsync("/api/donor/dashboard");
        Assert.Equal(HttpStatusCode.OK, afterReconcile.StatusCode);
    }

    private sealed record LoginEnvelope(string IdToken);
    private sealed record ErrorEnvelope(string ErrorCode, string Message, string TraceId);
    private sealed record MeEnvelope(string Email, string[] Roles);
}
