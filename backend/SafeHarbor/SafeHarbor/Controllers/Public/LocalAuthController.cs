using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SafeHarbor.Services.LocalAuth;

namespace SafeHarbor.Controllers.Public;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class LocalAuthController(
    IConfiguration configuration,
    IWebHostEnvironment environment,
    ILocalAccountStore localAccountStore) : ControllerBase
{
    internal static readonly HashSet<string> AllowedRoles = ["Admin", "SocialWorker", "Donor"];

    [HttpPost("local-register")]
    public IActionResult LocalRegister([FromBody] LocalRegisterRequest request)
    {
        var localAuthEnabled = environment.IsDevelopment() && configuration.GetValue<bool>("LocalAuth:Enabled");
        if (!localAuthEnabled)
        {
            return NotFound(new { error = "Local authentication is disabled." });
        }

        if (!localAccountStore.TryCreateAccount(request, out var registerError))
        {
            return BadRequest(new { error = registerError });
        }

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("local-login")]
    public ActionResult<LocalLoginResponse> LocalLogin([FromBody] LocalLoginRequest request)
    {
        var localAuthEnabled = environment.IsDevelopment() && configuration.GetValue<bool>("LocalAuth:Enabled");
        if (!localAuthEnabled)
        {
            // NOTE: This endpoint is intentionally disabled outside local-development auth mode
            // so production/staging continue using external identity provider sign-in only.
            return NotFound(new { error = "Local authentication is disabled." });
        }

        if (!localAccountStore.TryValidateCredentials(request, out var account, out var loginError))
        {
            return BadRequest(new { error = loginError });
        }

        var issuer = configuration["LocalAuth:Issuer"] ?? "safeharbor-local";
        var audience = configuration["LocalAuth:Audience"] ?? "safeharbor-local-client";
        var signingKey = configuration["LocalAuth:SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Local auth signing key is missing." });
        }

        var now = DateTime.UtcNow;
        var claims = new[]
        {
            new Claim(ClaimTypes.Email, account!.Email),
            new Claim("preferred_username", account.Email),
            new Claim(ClaimTypes.Role, account.Role),
            new Claim("role", account.Role),
            new Claim("sub", account.Email.ToLowerInvariant()),
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: now.AddHours(8),
            signingCredentials: credentials);

        return Ok(new LocalLoginResponse(new JwtSecurityTokenHandler().WriteToken(token)));
    }
}

public sealed record LocalLoginRequest(string Email, string Role, string Password);
public sealed record LocalRegisterRequest(string Email, string Role, string Password);
public sealed record LocalLoginResponse(string IdToken);
