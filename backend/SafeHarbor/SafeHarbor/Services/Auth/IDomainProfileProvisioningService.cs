namespace SafeHarbor.Services.Auth;

public interface IDomainProfileProvisioningService
{
    Task EnsureProvisionedForUserAsync(Guid userId, string email, string databaseRole, string? firstName, string? lastName, CancellationToken cancellationToken = default);
    Task<DomainProfileReconciliationResult> ReconcileAllAsync(CancellationToken cancellationToken = default);
}

public sealed record DomainProfileReconciliationResult(
    int UsersScanned,
    int DonorProfilesCreated,
    int UserProfilesCreated,
    int UserRoleLinksCreated,
    int RoleRowsCreated);
