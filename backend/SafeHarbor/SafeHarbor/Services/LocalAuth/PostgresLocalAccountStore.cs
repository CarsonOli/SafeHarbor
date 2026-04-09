using System.Security.Cryptography;
using Npgsql;
using SafeHarbor.Controllers.Public;

namespace SafeHarbor.Services.LocalAuth;

/// <summary>
/// PostgreSQL-backed account store for local auth.
/// Accounts are persisted in the auth_accounts table and survive server restarts.
/// </summary>
public sealed class PostgresLocalAccountStore(IConfiguration configuration) : ILocalAccountStore
{
    private static readonly HashSet<string> AllowedRoles = ["Admin", "SocialWorker", "Donor"];

    private NpgsqlConnection OpenConnection()
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");
        var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        return connection;
    }

    public bool TryCreateAccount(LocalRegisterRequest request, out string? error)
    {
        error = ValidateRequest(request.Email, request.Role, request.Password);
        if (error is not null) return false;

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var passwordHash = HashPassword(request.Password);

        try
        {
            using var connection = OpenConnection();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO auth_accounts (email, password_hash, role) VALUES (@email, @hash, @role)", connection);
            cmd.Parameters.AddWithValue("email", normalizedEmail);
            cmd.Parameters.AddWithValue("hash", passwordHash);
            cmd.Parameters.AddWithValue("role", request.Role);
            cmd.ExecuteNonQuery();
            return true;
        }
        catch (PostgresException ex) when (ex.SqlState == "23505") // unique_violation
        {
            error = "An account with that email already exists.";
            return false;
        }
    }

    public bool TryValidateCredentials(LocalLoginRequest request, out LocalAccountRecord? account, out string? error)
    {
        account = null;
        error = ValidateRequest(request.Email, request.Role, request.Password);
        if (error is not null) return false;

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var passwordHash = HashPassword(request.Password);

        using var connection = OpenConnection();
        using var cmd = new NpgsqlCommand(
            "SELECT email, role, password_hash FROM auth_accounts WHERE email = @email AND role = @role", connection);
        cmd.Parameters.AddWithValue("email", normalizedEmail);
        cmd.Parameters.AddWithValue("role", request.Role);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            error = "Invalid credentials.";
            return false;
        }

        var storedHash = reader.GetFieldValue<byte[]>(2);
        if (!CryptographicOperations.FixedTimeEquals(passwordHash, storedHash))
        {
            error = "Invalid credentials.";
            return false;
        }

        account = new LocalAccountRecord(reader.GetString(0), reader.GetString(1));
        return true;
    }

    private static string? ValidateRequest(string email, string role, string password)
    {
        if (string.IsNullOrWhiteSpace(email)) return "Email is required.";
        if (!AllowedRoles.Contains(role)) return $"Role must be one of: {string.Join(", ", AllowedRoles)}.";
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8) return "Password is required and must be at least 8 characters.";
        return null;
    }

    private static byte[] HashPassword(string password) =>
        SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password));
}
