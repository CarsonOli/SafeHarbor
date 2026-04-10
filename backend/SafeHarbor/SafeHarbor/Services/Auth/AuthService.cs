using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SafeHarbor.Auth;
using SafeHarbor.Data;
using SafeHarbor.Models.Entities;

namespace SafeHarbor.Services.Auth;

/// <summary>
/// User auth service backed by lighthouse.users.
/// </summary>
public sealed class AuthService(
    SafeHarborDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    IOptions<PasswordPolicyOptions> passwordPolicyOptions,
    IDomainProfileProvisioningService domainProfileProvisioningService) : IAuthService
{
    private static readonly HashSet<string> SupportedDatabaseRoles =
        ["admin", "staff", "user"];

    // NOTE: This alias map is the single source for accepted role vocabulary on inbound auth requests.
    // It intentionally allows both DB role values (admin/staff/user) and app policy roles
    // (Admin/SocialWorker/Donor) so existing frontend payloads remain compatible while preserving the
    // DB->app mapping contract used by JWT claims and policy checks.
    private static readonly Dictionary<string, string> RoleAliasToDatabaseRole = new(StringComparer.OrdinalIgnoreCase)
    {
        ["admin"] = "admin",
        ["staff"] = "staff",
        ["user"] = "user",
        ["Admin"] = "admin",
        ["SocialWorker"] = "staff",
        ["Donor"] = "user"
    };
    private readonly PasswordPolicyOptions _passwordPolicy = passwordPolicyOptions.Value;

    public async Task<AuthRegisterResult> RegisterAsync(RegisterAuthRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return new AuthRegisterResult(false, "ValidationError", "Email is required.");
        }

        var normalizedRole = NormalizeToDatabaseRole(request.Role);
        if (normalizedRole is null || !SupportedDatabaseRoles.Contains(normalizedRole))
        {
            return new AuthRegisterResult(false, "ValidationError", "Role must be one of: admin, staff, user.");
        }

        var passwordValidationError = ValidatePasswordAgainstPolicy(request.Password);
        if (passwordValidationError is not null)
        {
            return new AuthRegisterResult(false, "ValidationError", passwordValidationError);
        }

        // NOTE: Queries intentionally go through DbContext.Users which is schema-mapped to lighthouse.users
        // in SafeHarborDbContext.OnModelCreating. This keeps auth queries pinned to the expected table.
        var existingUser = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.Trim().ToLower() == normalizedEmail, cancellationToken);

        if (existingUser is not null)
        {
            return new AuthRegisterResult(false, "Conflict", "An account already exists for this email.");
        }

        var user = new User
        {
            UserId = Guid.NewGuid(),
            FirstName = request.FirstName?.Trim() ?? string.Empty,
            LastName = request.LastName?.Trim() ?? string.Empty,
            Email = normalizedEmail,
            Role = normalizedRole,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        // SECURITY: Only the framework-generated password hash is persisted; plaintext passwords are never stored.
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        dbContext.Users.Add(user);
        await domainProfileProvisioningService.EnsureProvisionedForUserAsync(
            user.UserId,
            user.Email,
            user.Role,
            user.FirstName,
            user.LastName,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthRegisterResult(true);
    }

    public Task<DomainProfileReconciliationResult> ReconcileDomainProfilesAsync(CancellationToken cancellationToken = default) =>
        domainProfileProvisioningService.ReconcileAllAsync(cancellationToken);

    public async Task<AuthAuthenticateResult> AuthenticateAsync(LoginAuthRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new AuthAuthenticateResult(false, ErrorCode: "ValidationError", Error: "Email and password are required.");
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.Trim().ToLower() == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return new AuthAuthenticateResult(false, ErrorCode: "InvalidCredentials", Error: "Invalid credentials.");
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return new AuthAuthenticateResult(false, ErrorCode: "InvalidCredentials", Error: "Invalid credentials.");
        }

        var mappedApiRoles = MapDatabaseRoleToApiRoles(user.Role);
        if (mappedApiRoles.Count == 0)
        {
            return new AuthAuthenticateResult(false, ErrorCode: "UnsupportedRole", Error: "Account role is not supported for API authorization.");
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var requestedRole = request.Role.Trim();
            var requestedDatabaseRole = NormalizeToDatabaseRole(requestedRole);

            var requestMatchesDatabaseRole = requestedDatabaseRole is not null
                && string.Equals(requestedDatabaseRole, user.Role, StringComparison.Ordinal);
            var requestMatchesMappedApiRole = mappedApiRoles.Contains(requestedRole, StringComparer.Ordinal);
            if (!requestMatchesDatabaseRole && !requestMatchesMappedApiRole)
            {
                return new AuthAuthenticateResult(false, ErrorCode: "ForbiddenRole", Error: "Requested role is not assigned to this account.");
            }
        }

        var profile = new AuthProfileResult(user.UserId, user.Email, user.Role, mappedApiRoles);
        var claims = BuildClaims(profile);

        return new AuthAuthenticateResult(true, profile, claims);
    }

    public async Task<AuthProfileResult?> LookupProfileByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return null;
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.Trim().ToLower() == normalizedEmail, cancellationToken);

        return user is null
            ? null
            : new AuthProfileResult(user.UserId, user.Email, user.Role, MapDatabaseRoleToApiRoles(user.Role));
    }

    private static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    private static string? NormalizeToDatabaseRole(string? inputRole)
    {
        var role = (inputRole ?? string.Empty).Trim();
        return RoleAliasToDatabaseRole.TryGetValue(role, out var normalizedRole)
            ? normalizedRole
            : null;
    }

    private string? ValidatePasswordAgainstPolicy(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return "Password is required.";
        }

        if (password.Length < _passwordPolicy.RequiredLength)
        {
            return $"Password must be at least {_passwordPolicy.RequiredLength} characters.";
        }

        // NOTE: Complexity checks are optional and configuration-driven. This preserves architecture
        // consistency with Identity policy settings while allowing lower-friction local development.
        if (_passwordPolicy.RequireDigit && !password.Any(char.IsDigit))
        {
            return "Password must contain at least one number.";
        }

        if (_passwordPolicy.RequireLowercase && !password.Any(char.IsLower))
        {
            return "Password must contain at least one lowercase letter.";
        }

        if (_passwordPolicy.RequireUppercase && !password.Any(char.IsUpper))
        {
            return "Password must contain at least one uppercase letter.";
        }

        if (_passwordPolicy.RequireNonAlphanumeric && !password.Any(c => !char.IsLetterOrDigit(c)))
        {
            return "Password must contain at least one special character.";
        }

        if (_passwordPolicy.RequiredUniqueChars > 0 && password.Distinct().Count() < _passwordPolicy.RequiredUniqueChars)
        {
            return $"Password must contain at least {_passwordPolicy.RequiredUniqueChars} unique characters.";
        }

        return null;
    }

    private static IReadOnlyCollection<string> MapDatabaseRoleToApiRoles(string role)
    {
        // NOTE: DB values remain lowercase domain roles; we emit API-facing auth claims in
        // the existing policy vocabulary to avoid broad authorization policy churn.
        return role switch
        {
            "admin" => ["Admin"],
            "staff" => ["SocialWorker"],
            "user" => ["Donor"],
            _ => []
        };
    }

    private static IReadOnlyCollection<Claim> BuildClaims(AuthProfileResult profile)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, profile.Email),
            new("preferred_username", profile.Email),
            new("sub", profile.UserId.ToString()),
            new("db_role", profile.DatabaseRole),
        };

        foreach (var role in profile.ApiRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("role", role));
            claims.Add(new Claim("roles", role));
        }

        return claims;
    }
}
