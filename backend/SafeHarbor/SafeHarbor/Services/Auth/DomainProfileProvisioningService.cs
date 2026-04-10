using Microsoft.EntityFrameworkCore;
using Npgsql;
using SafeHarbor.Data;
using SafeHarbor.Models.Entities;

namespace SafeHarbor.Services.Auth;

public sealed class DomainProfileProvisioningService(SafeHarborDbContext dbContext) : IDomainProfileProvisioningService
{
    public async Task EnsureProvisionedForUserAsync(Guid userId, string email, string databaseRole, string? firstName, string? lastName, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedEmail = NormalizeEmail(email);
            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                return;
            }

            if (string.Equals(databaseRole, "user", StringComparison.Ordinal))
            {
                await EnsureSupporterAsync(normalizedEmail, firstName, lastName, cancellationToken);
                return;
            }

            if (string.Equals(databaseRole, "staff", StringComparison.Ordinal) || string.Equals(databaseRole, "admin", StringComparison.Ordinal))
            {
                await EnsureStaffProfileAsync(userId, normalizedEmail, databaseRole, firstName, lastName, cancellationToken);
            }
        }
        catch (PostgresException ex) when (IsMissingDomainProfileSchema(ex))
        {
            // NOTE: Domain profile tables (supporters/user_profiles/roles/user_roles) may be absent in
            // partially provisioned environments. Registration should still succeed for lighthouse.users.
            // Operators can backfill profiles later via auth-maintenance reconciliation after schema sync.
            return;
        }
    }

    public async Task<DomainProfileReconciliationResult> ReconcileAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await dbContext.Users.AsNoTracking().ToListAsync(cancellationToken);

        var donorProfilesCreated = 0;
        var userProfilesCreated = 0;
        var userRoleLinksCreated = 0;
        var roleRowsCreated = 0;

        foreach (var user in users)
        {
            var counters = await EnsureProvisionedForUserInternalAsync(
                user.UserId,
                user.Email,
                user.Role,
                user.FirstName,
                user.LastName,
                cancellationToken);

            donorProfilesCreated += counters.DonorProfilesCreated;
            userProfilesCreated += counters.UserProfilesCreated;
            userRoleLinksCreated += counters.UserRoleLinksCreated;
            roleRowsCreated += counters.RoleRowsCreated;
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new DomainProfileReconciliationResult(
            users.Count,
            donorProfilesCreated,
            userProfilesCreated,
            userRoleLinksCreated,
            roleRowsCreated);
    }

    private async Task EnsureSupporterAsync(string normalizedEmail, string? firstName, string? lastName, CancellationToken cancellationToken)
    {
        // NOTE: Reconciliation can stage multiple inserts in one unit-of-work; we must check tracked
        // additions first so repeated emails in the same run remain idempotent before SaveChanges.
        var existingDonor = dbContext.Supporters.Local
            .FirstOrDefault(d => string.Equals(d.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
            ?? await dbContext.Supporters
                .FirstOrDefaultAsync(d => d.Email.ToLower() == normalizedEmail, cancellationToken);

        if (existingDonor is not null)
        {
            return;
        }

        var displayName = BuildDisplayName(normalizedEmail, firstName, lastName);
        dbContext.Supporters.Add(new Supporter
        {
            Id = Guid.NewGuid(),
            Name = displayName,
            DisplayName = displayName,
            Email = normalizedEmail,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            LastActivityAt = DateTimeOffset.UtcNow,
            LifetimeDonations = 0m
        });
    }

    private async Task EnsureStaffProfileAsync(Guid userId, string normalizedEmail, string databaseRole, string? firstName, string? lastName, CancellationToken cancellationToken)
    {
        var externalId = userId.ToString();
        var profile = dbContext.UserProfiles.Local
            .FirstOrDefault(p =>
                string.Equals(p.ExternalId, externalId, StringComparison.Ordinal)
                || string.Equals(p.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
            ?? await dbContext.UserProfiles
                .FirstOrDefaultAsync(p => p.ExternalId == externalId || p.Email.ToLower() == normalizedEmail, cancellationToken);

        if (profile is null)
        {
            profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                ExternalId = userId.ToString(),
                Email = normalizedEmail,
                DisplayName = BuildDisplayName(normalizedEmail, firstName, lastName),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            dbContext.UserProfiles.Add(profile);
        }

        var roleName = string.Equals(databaseRole, "admin", StringComparison.Ordinal) ? "Admin" : "SocialWorker";
        var role = dbContext.Roles.Local.FirstOrDefault(r => string.Equals(r.Name, roleName, StringComparison.Ordinal))
            ?? await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);
        if (role is null)
        {
            role = new Role
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            dbContext.Roles.Add(role);
        }

        var hasRoleLink = await dbContext.UserRoles
            .AnyAsync(ur => ur.UserProfileId == profile.Id && ur.RoleId == role.Id, cancellationToken);
        if (!hasRoleLink)
        {
            dbContext.UserRoles.Add(new UserRole
            {
                Id = Guid.NewGuid(),
                UserProfileId = profile.Id,
                RoleId = role.Id
            });
        }
    }

    private async Task<ProvisioningCounters> EnsureProvisionedForUserInternalAsync(Guid userId, string email, string databaseRole, string? firstName, string? lastName, CancellationToken cancellationToken)
    {
        var counters = new ProvisioningCounters();
        var beforeDonors = dbContext.ChangeTracker.Entries<Supporter>().Count(e => e.State == EntityState.Added);
        var beforeProfiles = dbContext.ChangeTracker.Entries<UserProfile>().Count(e => e.State == EntityState.Added);
        var beforeRoles = dbContext.ChangeTracker.Entries<Role>().Count(e => e.State == EntityState.Added);
        var beforeLinks = dbContext.ChangeTracker.Entries<UserRole>().Count(e => e.State == EntityState.Added);

        await EnsureProvisionedForUserAsync(userId, email, databaseRole, firstName, lastName, cancellationToken);

        counters.DonorProfilesCreated = dbContext.ChangeTracker.Entries<Supporter>().Count(e => e.State == EntityState.Added) - beforeDonors;
        counters.UserProfilesCreated = dbContext.ChangeTracker.Entries<UserProfile>().Count(e => e.State == EntityState.Added) - beforeProfiles;
        counters.RoleRowsCreated = dbContext.ChangeTracker.Entries<Role>().Count(e => e.State == EntityState.Added) - beforeRoles;
        counters.UserRoleLinksCreated = dbContext.ChangeTracker.Entries<UserRole>().Count(e => e.State == EntityState.Added) - beforeLinks;

        return counters;
    }

    private static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    private static bool IsMissingDomainProfileSchema(PostgresException ex) =>
        ex.SqlState == PostgresErrorCodes.UndefinedTable || ex.SqlState == PostgresErrorCodes.UndefinedColumn;

    // NOTE: We use a deterministic fallback display name from email local-part so supporter/staff
    // records are immediately queryable even when registration omits first/last names.
    private static string BuildDisplayName(string normalizedEmail, string? firstName, string? lastName)
    {
        var displayName = $"{firstName} {lastName}".Trim();
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName;
        }

        var localPart = normalizedEmail.Split('@', 2, StringSplitOptions.TrimEntries)[0];
        return string.IsNullOrWhiteSpace(localPart) ? normalizedEmail : localPart;
    }

    private sealed class ProvisioningCounters
    {
        public int DonorProfilesCreated { get; set; }
        public int UserProfilesCreated { get; set; }
        public int UserRoleLinksCreated { get; set; }
        public int RoleRowsCreated { get; set; }
    }
}


