namespace SafeHarbor.Auth;

/// <summary>
/// Explicit password/lockout values used by ASP.NET Identity.
/// Keeping this in configuration avoids hidden framework defaults across environments.
/// </summary>
public sealed class PasswordPolicyOptions
{
    public const string SectionName = "Identity:PasswordPolicy";

    public int RequiredLength { get; set; } = 12;
    public int RequiredUniqueChars { get; set; } = 3;
    public bool RequireDigit { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireNonAlphanumeric { get; set; } = true;
    public int MaxFailedAccessAttempts { get; set; } = 5;
    public int DefaultLockoutMinutes { get; set; } = 15;
}
