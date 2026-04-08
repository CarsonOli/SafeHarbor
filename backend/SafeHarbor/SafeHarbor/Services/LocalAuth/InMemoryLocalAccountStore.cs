using System.Collections.Concurrent;
using System.Security.Cryptography;
using SafeHarbor.Controllers.Public;

namespace SafeHarbor.Services.LocalAuth;

public sealed class InMemoryLocalAccountStore : ILocalAccountStore
{
    private readonly ConcurrentDictionary<string, StoredLocalAccount> _accounts = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryLocalAccountStore()
    {
        // NOTE: Seeded accounts keep local onboarding friction low for teammates who
        // just pulled the branch and want to validate login immediately.
        SeedIfMissing("alice@example.com", "Donor", "Password123!");
        SeedIfMissing("bob@example.com", "Donor", "Password123!");
        SeedIfMissing("admin@safeharbor.local", "Admin", "Password123!");
        SeedIfMissing("socialworker@safeharbor.local", "SocialWorker", "Password123!");
    }

    public bool TryCreateAccount(LocalRegisterRequest request, out string? error)
    {
        error = ValidateRequest(request.Email, request.Role, request.Password);
        if (error is not null)
        {
            return false;
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var passwordHash = HashPassword(request.Password.Trim());
        var created = _accounts.TryAdd(normalizedEmail, new StoredLocalAccount(request.Email.Trim(), request.Role, passwordHash));
        if (!created)
        {
            error = "An account already exists for this email.";
            return false;
        }

        return true;
    }

    public bool TryValidateCredentials(LocalLoginRequest request, out LocalAccountRecord? account, out string? error)
    {
        account = null;
        error = ValidateRequest(request.Email, request.Role, request.Password);
        if (error is not null)
        {
            return false;
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (!_accounts.TryGetValue(normalizedEmail, out var storedAccount))
        {
            error = "No local account found for this email. Create an account first.";
            return false;
        }

        // NOTE: Role checks are enforced here so local role simulation cannot silently
        // bypass authorization assumptions made by protected endpoints.
        if (!string.Equals(storedAccount.Role, request.Role, StringComparison.Ordinal))
        {
            error = $"This account is registered as {storedAccount.Role}. Choose that role to sign in.";
            return false;
        }

        var providedPasswordHash = HashPassword(request.Password.Trim());
        if (!CryptographicOperations.FixedTimeEquals(storedAccount.PasswordHash, providedPasswordHash))
        {
            error = "Incorrect password.";
            return false;
        }

        account = new LocalAccountRecord(storedAccount.Email, storedAccount.Role);
        return true;
    }

    private void SeedIfMissing(string email, string role, string password)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        _accounts.TryAdd(normalizedEmail, new StoredLocalAccount(email, role, HashPassword(password)));
    }

    private static string? ValidateRequest(string email, string role, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "Email is required.";
        }

        if (string.IsNullOrWhiteSpace(role) || !LocalAuthController.AllowedRoles.Contains(role))
        {
            return "A supported role is required.";
        }

        if (string.IsNullOrWhiteSpace(password) || password.Trim().Length < 8)
        {
            return "Password is required and must be at least 8 characters.";
        }

        return null;
    }

    private static byte[] HashPassword(string password)
    {
        // NOTE: Local auth is development-only, but hashing still protects against
        // accidental credential exposure in logs/memory dumps during debugging.
        return SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password));
    }

    private sealed record StoredLocalAccount(string Email, string Role, byte[] PasswordHash);
}
