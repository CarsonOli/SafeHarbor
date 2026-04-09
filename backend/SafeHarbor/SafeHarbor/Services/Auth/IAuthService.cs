using System.Security.Claims;

namespace SafeHarbor.Services.Auth;

public interface IAuthService
{
    Task<AuthRegisterResult> RegisterAsync(RegisterAuthRequest request, CancellationToken cancellationToken = default);
    Task<AuthAuthenticateResult> AuthenticateAsync(LoginAuthRequest request, CancellationToken cancellationToken = default);
    Task<AuthProfileResult?> LookupProfileByEmailAsync(string email, CancellationToken cancellationToken = default);
}

public sealed record RegisterAuthRequest(string Email, string Role, string Password, string? FirstName = null, string? LastName = null);
public sealed record LoginAuthRequest(string Email, string Password, string? Role = null);

public sealed record AuthRegisterResult(bool Succeeded, string? ErrorCode = null, string? Error = null);

public sealed record AuthAuthenticateResult(
    bool Succeeded,
    AuthProfileResult? Profile = null,
    IReadOnlyCollection<Claim>? Claims = null,
    string? ErrorCode = null,
    string? Error = null);

public sealed record AuthProfileResult(Guid UserId, string Email, string DatabaseRole, IReadOnlyCollection<string> ApiRoles);
