using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SafeHarbor.Data;
using SafeHarbor.Models.Entities;

namespace SafeHarbor.Services.Auth;

/// <summary>
/// User auth service backed by lighthouse.users.
/// </summary>
public sealed class AuthService(
    SafeHarborDbContext dbContext,
    IPasswordHasher<User> passwordHasher) : IAuthService
{
    private static readonly HashSet<string> SupportedDatabaseRoles =
        ["admin", "staff", "user"];

    public async Task<AuthRegisterResult> RegisterAsync(RegisterAuthRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return new AuthRegisterResult(false, "Email is required.");
        }

        var normalizedRole = request.Role.Trim().ToLowerInvariant();
        if (!SupportedDatabaseRoles.Contains(normalizedRole))
        {
            return new AuthRegisterResult(false, "Role must be one of: admin, staff, user.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return new AuthRegisterResult(false, "Password is required and must be at least 8 characters.");
        }

        // NOTE: Queries intentionally go through DbContext.Users which is schema-mapped to lighthouse.users
        // in SafeHarborDbContext.OnModelCreating. This keeps auth queries pinned to the expected table.
        var existingUser = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.Trim().ToLower() == normalizedEmail, cancellationToken);

        if (existingUser is not null)
        {
            return new AuthRegisterResult(false, "An account already exists for this email.");
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

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthRegisterResult(true);
    }

    public async Task<AuthAuthenticateResult> AuthenticateAsync(LoginAuthRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new AuthAuthenticateResult(false, Error: "Email and password are required.");
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.Trim().ToLower() == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return new AuthAuthenticateResult(false, Error: "Invalid credentials.");
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return new AuthAuthenticateResult(false, Error: "Invalid credentials.");
        }

        var mappedApiRoles = MapDatabaseRoleToApiRoles(user.Role);
        if (mappedApiRoles.Count == 0)
        {
            return new AuthAuthenticateResult(false, Error: "Account role is not supported for API authorization.");
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var requestedRole = request.Role.Trim();
            var requestedDatabaseRole = requestedRole.ToLowerInvariant();

            var requestMatchesDatabaseRole = string.Equals(requestedDatabaseRole, user.Role, StringComparison.Ordinal);
            var requestMatchesMappedApiRole = mappedApiRoles.Contains(requestedRole, StringComparer.Ordinal);
            if (!requestMatchesDatabaseRole && !requestMatchesMappedApiRole)
            {
                return new AuthAuthenticateResult(false, Error: "Requested role is not assigned to this account.");
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
