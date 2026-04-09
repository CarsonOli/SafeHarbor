using Microsoft.AspNetCore.Identity;

namespace SafeHarbor.Auth;

/// <summary>
/// Identity-backed application user for local-development auth and future first-party login paths.
/// We keep Guid keys to align with existing domain entity key conventions.
/// </summary>
public sealed class AppUser : IdentityUser<Guid>
{
}
