using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace SafeHarbor.Controllers.Public;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class LocalAuthController(IConfiguration configuration, IWebHostEnvironment environment) : ControllerBase
{
    private static readonly HashSet<string> AllowedRoles = ["Admin", "SocialWorker", "Donor"];

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

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { error = "Email is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Role) || !AllowedRoles.Contains(request.Role))
        {
            return BadRequest(new { error = "A supported role is required." });
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
            new Claim(ClaimTypes.Email, request.Email.Trim()),
            new Claim("preferred_username", request.Email.Trim()),
            new Claim(ClaimTypes.Role, request.Role),
            new Claim("role", request.Role),
            new Claim("sub", request.Email.Trim().ToLowerInvariant()),
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

public sealed record LocalLoginRequest(string Email, string Role);
public sealed record LocalLoginResponse(string IdToken);
