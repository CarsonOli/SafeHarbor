using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SafeHarbor.Auth;

namespace SafeHarbor.Controllers.Public;

[ApiController]
[Route("api/auth")]
public sealed class LocalAuthController(
    IConfiguration configuration,
    IWebHostEnvironment environment,
    UserManager<AppUser> userManager) : ControllerBase
{
    internal static readonly HashSet<string> AllowedRoles = ["Admin", "SocialWorker", "Donor"];

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!IsLocalAuthEnabled())
        {
            return NotFound(new { error = "Local authentication is disabled." });
        }

        if (!AllowedRoles.Contains(request.Role))
        {
            return BadRequest(new { error = "A supported role is required." });
        }

        var existingUser = await userManager.FindByEmailAsync(request.Email.Trim());
        if (existingUser is not null)
        {
            return BadRequest(new { error = "An account already exists for this email." });
        }

        var user = new AppUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            EmailConfirmed = true,
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            // NOTE: Surface identity validator output so clients can correct password policy failures
            // without reverse-engineering backend rules.
            var message = string.Join("; ", createResult.Errors.Select(error => error.Description));
            return BadRequest(new { error = message });
        }

        var roleResult = await userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            var message = string.Join("; ", roleResult.Errors.Select(error => error.Description));
            return BadRequest(new { error = message });
        }

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (!IsLocalAuthEnabled())
        {
            return NotFound(new { error = "Local authentication is disabled." });
        }

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return BadRequest(new { error = "No local account found for this email. Create an account first." });
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return BadRequest(new { error = "Account is locked due to repeated failed logins. Try again later." });
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            await userManager.AccessFailedAsync(user);
            return BadRequest(new { error = "Incorrect password." });
        }

        await userManager.ResetAccessFailedCountAsync(user);
        var roles = await userManager.GetRolesAsync(user);

        if (request.Role is not null && !roles.Contains(request.Role, StringComparer.Ordinal))
        {
            return BadRequest(new { error = "Requested role is not assigned to this account." });
        }

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var issuer = jwtOptions.Issuer ?? "safeharbor-local";
        var audience = jwtOptions.Audience ?? "safeharbor-local-client";
        var signingKey = jwtOptions.SigningKey;
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "JWT signing key is missing." });
        }

        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, user.Email ?? request.Email.Trim()),
            new("preferred_username", user.Email ?? request.Email.Trim()),
            new("sub", user.Id.ToString()),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("role", role));
            claims.Add(new Claim("roles", role));
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var tokenLifetimeMinutes = jwtOptions.TokenLifetimeMinutes > 0 ? jwtOptions.TokenLifetimeMinutes : 480;

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(tokenLifetimeMinutes),
            signingCredentials: credentials);

        return Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token)));
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<MeResponse> Me()
    {
        var email = User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue("preferred_username")
            ?? User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized();
        }

        var roles = User.Claims
            .Where(claim => claim.Type is ClaimTypes.Role or "role" or "roles")
            .Select(claim => claim.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return Ok(new MeResponse(email, roles));
    }

    // Legacy aliases kept for compatibility with existing frontend and tests while clients migrate.
    [HttpPost("local-register")]
    [AllowAnonymous]
    public Task<IActionResult> LocalRegister([FromBody] RegisterRequest request) => Register(request);

    [HttpPost("local-login")]
    [AllowAnonymous]
    public Task<ActionResult<LoginResponse>> LocalLogin([FromBody] LoginRequest request) => Login(request);

    private bool IsLocalAuthEnabled() => environment.IsDevelopment() && configuration.GetValue<bool>("LocalAuth:Enabled");
}

public sealed record LoginRequest(string Email, string Password, string? Role = null);
public sealed record RegisterRequest(string Email, string Role, string Password);
public sealed record LoginResponse(string IdToken);
public sealed record MeResponse(string Email, IReadOnlyCollection<string> Roles);
