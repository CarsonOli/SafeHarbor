using Microsoft.EntityFrameworkCore;
using SafeHarbor.Models.Entities;
using SafeHarbor.Models.Enums;
using SafeHarbor.Models.Lookups;

namespace SafeHarbor.Data;

public class SafeHarborDbContext(DbContextOptions<SafeHarborDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Safehouse> Safehouses => Set<Safehouse>();
    public DbSet<ResidentCase> ResidentCases => Set<ResidentCase>();
    public DbSet<ResidentAssessment> ResidentAssessments => Set<ResidentAssessment>();
    public DbSet<ProcessRecording> ProcessRecordings => Set<ProcessRecording>();
    public DbSet<HomeVisit> HomeVisits => Set<HomeVisit>();
    public DbSet<CaseConference> CaseConferences => Set<CaseConference>();
    public DbSet<InterventionPlan> InterventionPlans => Set<InterventionPlan>();
    public DbSet<Donor> Donors => Set<Donor>();
    public DbSet<Contribution> Contributions => Set<Contribution>();
    public DbSet<ContributionAllocation> ContributionAllocations => Set<ContributionAllocation>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<SocialPostMetric> SocialPostMetrics => Set<SocialPostMetric>();
    public DbSet<OutcomeSnapshot> OutcomeSnapshots => Set<OutcomeSnapshot>();

    public DbSet<CaseCategory> CaseCategories => Set<CaseCategory>();
    public DbSet<CaseSubcategory> CaseSubcategories => Set<CaseSubcategory>();
    public DbSet<VisitType> VisitTypes => Set<VisitType>();
    public DbSet<ContributionType> ContributionTypes => Set<ContributionType>();
    public DbSet<StatusState> StatusStates => Set<StatusState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserRole>().HasKey(x => new { x.UserId, x.RoleId });

        modelBuilder.Entity<ResidentCase>().HasIndex(x => x.StatusStateId);
        modelBuilder.Entity<ResidentCase>().HasIndex(x => x.SafehouseId);
        modelBuilder.Entity<ResidentCase>().HasIndex(x => x.CaseCategoryId);
        modelBuilder.Entity<CaseConference>().HasIndex(x => x.ConferenceDate);
        modelBuilder.Entity<Donor>().HasIndex(x => x.LastActivityAt);
        modelBuilder.Entity<Contribution>().HasIndex(x => x.ContributionDate);

        modelBuilder.Entity<User>().HasIndex(x => x.ExternalId).IsUnique();
        modelBuilder.Entity<Role>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<CaseCategory>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<CaseSubcategory>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<VisitType>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<ContributionType>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<StatusState>().HasIndex(x => new { x.Domain, x.Code }).IsUnique();

        modelBuilder.Entity<CaseCategory>().HasData(
            LookupSeeders.CaseCategories.Select(x => new CaseCategory
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                CreatedAt = LookupSeeders.SeededAt,
                UpdatedAt = LookupSeeders.SeededAt,
                CreatedBy = "seed"
            }));

        modelBuilder.Entity<CaseSubcategory>().HasData(
            LookupSeeders.CaseSubcategories.Select(x => new CaseSubcategory
            {
                Id = x.Id,
                CaseCategoryId = x.CaseCategoryId,
                Code = x.Code,
                Name = x.Name,
                CreatedAt = LookupSeeders.SeededAt,
                UpdatedAt = LookupSeeders.SeededAt,
                CreatedBy = "seed"
            }));

        modelBuilder.Entity<VisitType>().HasData(
            LookupSeeders.VisitTypes.Select(x => new VisitType
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                CreatedAt = LookupSeeders.SeededAt,
                UpdatedAt = LookupSeeders.SeededAt,
                CreatedBy = "seed"
            }));

        modelBuilder.Entity<ContributionType>().HasData(
            LookupSeeders.ContributionTypes.Select(x => new ContributionType
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                CreatedAt = LookupSeeders.SeededAt,
                UpdatedAt = LookupSeeders.SeededAt,
                CreatedBy = "seed"
            }));

        modelBuilder.Entity<StatusState>().HasData(
            LookupSeeders.StatusStates.Select(x => new StatusState
            {
                Id = x.Id,
                Domain = x.Domain,
                Code = x.Code,
                Name = x.Name,
                CreatedAt = LookupSeeders.SeededAt,
                UpdatedAt = LookupSeeders.SeededAt,
                CreatedBy = "seed"
            }));

        ApplySoftDeleteFilter(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditing();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditing();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditing()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.CreatedBy = string.IsNullOrWhiteSpace(entry.Entity.CreatedBy)
                        ? "system"
                        : entry.Entity.CreatedBy;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.DeletedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }
    }

    private static void ApplySoftDeleteFilter(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<Role>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<Safehouse>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<ResidentCase>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<ResidentAssessment>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<ProcessRecording>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<HomeVisit>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<CaseConference>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<InterventionPlan>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<Donor>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<Contribution>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<ContributionAllocation>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<Campaign>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<SocialPostMetric>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<OutcomeSnapshot>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<CaseCategory>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<CaseSubcategory>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<VisitType>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<ContributionType>().HasQueryFilter(x => x.DeletedAt == null);
        modelBuilder.Entity<StatusState>().HasQueryFilter(x => x.DeletedAt == null);
    }
}
