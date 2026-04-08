using SafeHarbor.Controllers.Public;

namespace SafeHarbor.Services.LocalAuth;

public interface ILocalAccountStore
{
    bool TryCreateAccount(LocalRegisterRequest request, out string? error);
    bool TryValidateCredentials(LocalLoginRequest request, out LocalAccountRecord? account, out string? error);
}

public sealed record LocalAccountRecord(string Email, string Role);
