using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SafeHarbor.Tests;

/// <summary>
/// Lightweight header-driven auth scheme used by integration tests.
/// This keeps authorization tests deterministic without depending on real JWT issuance.
/// </summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string Scheme = "TestAuth";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Auth", out var authenticatedHeader)
            || !string.Equals(authenticatedHeader.ToString(), "true", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing test authentication header."));
        }

        var claims = new List<Claim>();

        var oid = Request.Headers["X-Test-Oid"].ToString();
        if (!string.IsNullOrWhiteSpace(oid))
        {
            claims.Add(new Claim("oid", oid));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, oid));
        }

        var email = Request.Headers["X-Test-Email"].ToString();
        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
            claims.Add(new Claim("preferred_username", email));
        }

        var role = Request.Headers["X-Test-Role"].ToString();
        if (!string.IsNullOrWhiteSpace(role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
