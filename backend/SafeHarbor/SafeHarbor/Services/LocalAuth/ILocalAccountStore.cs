namespace SafeHarbor.Services.LocalAuth;

// NOTE: Local in-memory auth has been superseded by ASP.NET Identity-backed local development auth.
// Keep this marker interface/record to avoid broad file churn during migration; it is intentionally unused.
public interface ILocalAccountStore;

public sealed record LocalAccountRecord(string Email, string Role);
