namespace SafeHarbor.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string? Issuer { get; init; }

    public string? Audience { get; init; }

    public string? SigningKey { get; init; }

    // Keep token lifetime configurable so local/dev/demo environments can tune expiry
    // without changing token issuance or API validation code paths.
    public int TokenLifetimeMinutes { get; init; } = 480;
}
