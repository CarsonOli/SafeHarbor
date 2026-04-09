using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SafeHarbor.Auth;
using SafeHarbor.DTOs;
using SafeHarbor.Services.Auth;

namespace SafeHarbor.Controllers.Public;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IConfiguration configuration,
    IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        if (!IsLocalAuthEnabled())
        {
            return NotFound(CreateErrorEnvelope("FeatureDisabled", "Local authentication is disabled."));
        }

        var registerResult = await authService.RegisterAsync(
            new RegisterAuthRequest(request.Email, request.Role, request.Password),
            cancellationToken);

        if (!registerResult.Succeeded)
        {
            var errorCode = registerResult.ErrorCode ?? "RegistrationFailed";
            var message = registerResult.Error ?? "Registration failed.";
            var statusCode = string.Equals(errorCode, "Conflict", StringComparison.Ordinal) ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest;
            return StatusCode(statusCode, CreateErrorEnvelope(errorCode, message));
        }

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (!IsLocalAuthEnabled())
        {
            return NotFound(CreateErrorEnvelope("FeatureDisabled", "Local authentication is disabled."));
        }

        var authResult = await authService.AuthenticateAsync(
            new LoginAuthRequest(request.Email, request.Password, request.Role),
            cancellationToken);

        if (!authResult.Succeeded || authResult.Claims is null || authResult.Profile is null)
        {
            var errorCode = authResult.ErrorCode ?? "AuthenticationFailed";
            var message = authResult.Error ?? "Invalid credentials.";
            var statusCode = string.Equals(errorCode, "ForbiddenRole", StringComparison.Ordinal)
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status400BadRequest;
            return StatusCode(statusCode, CreateErrorEnvelope(errorCode, message));
        }

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var issuer = jwtOptions.Issuer ?? "safeharbor-local";
        var audience = jwtOptions.Audience ?? "safeharbor-local-client";
        var signingKey = jwtOptions.SigningKey;
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, CreateErrorEnvelope("ConfigurationError", "JWT signing key is missing."));
        }

        var now = DateTime.UtcNow;
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var tokenLifetimeMinutes = jwtOptions.TokenLifetimeMinutes > 0 ? jwtOptions.TokenLifetimeMinutes : 480;

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: authResult.Claims,
            notBefore: now,
            expires: now.AddMinutes(tokenLifetimeMinutes),
            signingCredentials: credentials);

        return Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token)));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeResponse>> Me(CancellationToken cancellationToken)
    {
        var email = User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue("preferred_username")
            ?? User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized();
        }

        var profile = await authService.LookupProfileByEmailAsync(email, cancellationToken);
        if (profile is null)
        {
            return Unauthorized();
        }

        return Ok(new MeResponse(profile.Email, profile.ApiRoles));
    }

    // Legacy aliases kept for compatibility with existing frontend and tests while clients migrate.
    [HttpPost("local-register")]
    [AllowAnonymous]
    public Task<IActionResult> LocalRegister([FromBody] RegisterRequest request, CancellationToken cancellationToken) => Register(request, cancellationToken);

    [HttpPost("local-login")]
    [AllowAnonymous]
    public Task<ActionResult<LoginResponse>> LocalLogin([FromBody] LoginRequest request, CancellationToken cancellationToken) => Login(request, cancellationToken);

    // Local auth availability is config-driven so staging/production can opt in explicitly
    // for first-party email/password flows without requiring a Development environment.
    private bool IsLocalAuthEnabled() => configuration.GetValue<bool>("LocalAuth:Enabled");

    private ApiErrorEnvelope CreateErrorEnvelope(string errorCode, string message) =>
        new(errorCode, message, HttpContext.TraceIdentifier);
}

public sealed record LoginRequest(
    [param: Required, EmailAddress] string Email,
    [param: Required, MinLength(8)] string Password,
    string? Role = null);

public sealed record RegisterRequest(
    [param: Required, EmailAddress] string Email,
    [param: Required] string Role,
    // NOTE: Base minimum-length validation is API-contract level; full complexity is enforced
    // in AuthService from configured PasswordPolicyOptions to avoid duplicating policy constants.
    [param: Required, MinLength(8)] string Password);
public sealed record LoginResponse(string IdToken);
public sealed record MeResponse(string Email, IReadOnlyCollection<string> Roles);
