using Microsoft.EntityFrameworkCore;
using SafeHarbor.Models;
using SafeHarbor.Models.Entities;
using SafeHarbor.Models.Lookups;

namespace SafeHarbor.Data
{
    public class SafeHarborDbContext : DbContext
    {
        // NOTE: We standardize all operational entities to a single canonical schema
        // to keep logged-in page queries and migrations deterministic across environments.
        private const string CanonicalSchema = "lighthouse";

        public SafeHarborDbContext(DbContextOptions<SafeHarborDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<StatusState> StatusState { get; set; }
        public DbSet<CaseCategory> CaseCategories { get; set; }

        // --- The Rest of the 17 ---
        public DbSet<Resident> Residents { get; set; }
        public DbSet<ResidentCase> ResidentCases { get; set; }
        public DbSet<ResidentAssessment> ResidentAssessments { get; set; }
        public DbSet<InterventionPlan> InterventionPlans { get; set; }
        public DbSet<HomeVisit> HomeVisits { get; set; }
        public DbSet<CaseConference> CaseConferences { get; set; }
        public DbSet<ProcessRecording> ProcessRecordings { get; set; }
        public DbSet<Safehouse> Safehouses { get; set; }

        // --- Fundraising ---
        public DbSet<Supporter> Supporters { get; set; }
        public DbSet<Campaign> Campaigns { get; set; }
        public DbSet<Contribution> Contributions { get; set; }
        public DbSet<ContributionAllocation> ContributionAllocations { get; set; }
        public DbSet<SocialPostMetric> SocialPostMetrics { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(CanonicalSchema);

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users", CanonicalSchema);
                entity.HasKey(u => u.UserId);

                entity.Property(u => u.UserId).HasColumnName("user_id");
                entity.Property(u => u.SupporterId).HasColumnName("supporter_id");
                entity.Property(u => u.FirstName).HasColumnName("f_name");
                entity.Property(u => u.LastName).HasColumnName("l_name");
                entity.Property(u => u.Email).HasColumnName("email");
                entity.Property(u => u.Role).HasColumnName("role");
                entity.Property(u => u.PasswordHash).HasColumnName("password_hash");
                entity.Property(u => u.CreatedAt).HasColumnName("created_at");
                entity.Property(u => u.UpdatedAt).HasColumnName("updated_at");
            });

            modelBuilder.Entity<Resident>(entity =>
            {
                entity.ToTable("residents", CanonicalSchema);
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.FullName).HasColumnName("full_name");
                entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
                entity.Property(e => e.MedicalNotes).HasColumnName("medical_notes");
                entity.Property(e => e.CaseWorkerEmail).HasColumnName("case_worker_email");
                entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
                entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            });

            modelBuilder.Entity<Supporter>(entity =>
            {
                entity.ToTable("supporters", CanonicalSchema);
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.DisplayName).HasColumnName("display_name");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.LifetimeDonations).HasColumnName("lifetime_donations").HasPrecision(18, 2);
                entity.Property(e => e.PaymentToken).HasColumnName("payment_token");
                entity.Property(e => e.LastActivityAt).HasColumnName("last_activity_at");
                entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
                entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
                MapAuditColumns(entity);
            });

            modelBuilder.Entity<Campaign>(entity =>
            {
                entity.ToTable("campaigns", CanonicalSchema);
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.StartDate).HasColumnName("start_date");
                entity.Property(e => e.EndDate).HasColumnName("end_date");
                entity.Property(e => e.StatusStateId).HasColumnName("status_state_id");
                entity.Property(e => e.GoalAmount).HasColumnName("goal_amount");
                MapAuditColumns(entity);
            });

            modelBuilder.Entity<Contribution>(entity =>
            {
                entity.ToTable("contributions", CanonicalSchema);
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.SupporterId).HasColumnName("supporter_id");
                entity.Property(e => e.CampaignId).HasColumnName("campaign_id");
                entity.Property(e => e.ContributionTypeId).HasColumnName("contribution_type_id");
                entity.Property(e => e.StatusStateId).HasColumnName("status_state_id");
                entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(18, 2);
                entity.Property(e => e.Frequency).HasColumnName("frequency");
                entity.Property(e => e.ContributionDate).HasColumnName("contribution_date");
                MapAuditColumns(entity);
            });

            modelBuilder.Entity<ContributionAllocation>(entity =>
            {
                entity.ToTable("contribution_allocations", CanonicalSchema);
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ContributionId).HasColumnName("contribution_id");
                entity.Property(e => e.SafehouseId).HasColumnName("safehouse_id");
                entity.Property(e => e.AmountAllocated).HasColumnName("amount_allocated").HasPrecision(18, 2);
                MapAuditColumns(entity);
            });

            modelBuilder.Entity<SocialPostMetric>(entity =>
            {
                entity.ToTable("social_post_metrics", CanonicalSchema);
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.CampaignId).HasColumnName("campaign_id");
                entity.Property(e => e.PostedAt).HasColumnName("posted_at");
                entity.Property(e => e.Platform).HasColumnName("platform");
                entity.Property(e => e.ContentType).HasColumnName("content_type");
                entity.Property(e => e.Reach).HasColumnName("reach");
                entity.Property(e => e.Engagements).HasColumnName("engagements");
                entity.Property(e => e.AttributedDonationAmount).HasColumnName("attributed_donation_amount").HasPrecision(18, 2);
                entity.Property(e => e.AttributedDonationCount).HasColumnName("attributed_donation_count");
                MapAuditColumns(entity);
            });

            modelBuilder.Entity<Safehouse>(entity =>
            {
                entity.ToTable("safehouses", CanonicalSchema);
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Region).HasColumnName("region");
                MapAuditColumns(entity);
            });

            modelBuilder.Entity<ResidentCase>(entity =>
            {
                entity.ToTable("resident_cases", CanonicalSchema);
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.SafehouseId).HasColumnName("safehouse_id");
                entity.Property(e => e.ResidentId).HasColumnName("resident_id");
                entity.Property(e => e.CaseCategoryId).HasColumnName("case_category_id");
                entity.Property(e => e.CaseSubcategoryId).HasColumnName("case_subcategory_id");
                entity.Property(e => e.StatusStateId).HasColumnName("status_state_id");
                entity.Property(e => e.OpenedAt).HasColumnName("opened_at");
                entity.Property(e => e.ClosedAt).HasColumnName("closed_at");
                MapAuditColumns(entity);
            });

            modelBuilder.Entity<ProcessRecording>(entity =>
            {
                entity.ToTable("process_recordings", CanonicalSchema);
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ResidentCaseId).HasColumnName("resident_case_id");
                entity.Property(e => e.RecordedAt).HasColumnName("recorded_at");
                entity.Property(e => e.Summary).HasColumnName("summary");
                MapAuditColumns(entity);
            });

            modelBuilder.Entity<HomeVisit>(entity =>
            {
                entity.ToTable("home_visits", CanonicalSchema);
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ResidentCaseId).HasColumnName("resident_case_id");
                entity.Property(e => e.VisitTypeId).HasColumnName("visit_type_id");
                entity.Property(e => e.StatusStateId).HasColumnName("status_state_id");
                entity.Property(e => e.VisitDate).HasColumnName("visit_date");
                entity.Property(e => e.HomeEnvironmentObservations).HasColumnName("home_environment_observations");
                entity.Property(e => e.FamilyCooperationLevel).HasColumnName("family_cooperation_level");
                entity.Property(e => e.SafetyConcernsIdentified).HasColumnName("safety_concerns_identified");
                entity.Property(e => e.FollowUpActions).HasColumnName("follow_up_actions");
                entity.Property(e => e.Notes).HasColumnName("notes");
                MapAuditColumns(entity);
            });

            modelBuilder.Entity<CaseConference>(entity =>
            {
                entity.ToTable("case_conferences", CanonicalSchema);
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ResidentCaseId).HasColumnName("resident_case_id");
                entity.Property(e => e.ConferenceDate).HasColumnName("conference_date");
                entity.Property(e => e.StatusStateId).HasColumnName("status_state_id");
                entity.Property(e => e.OutcomeSummary).HasColumnName("outcome_summary");
                MapAuditColumns(entity);
            });

            modelBuilder.Entity<ResidentAssessment>(entity =>
            {
                entity.ToTable("resident_assessments", CanonicalSchema);
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ResidentCaseId).HasColumnName("resident_case_id");
                entity.Property(e => e.AssessedAt).HasColumnName("assessed_at");
                entity.Property(e => e.StatusStateId).HasColumnName("status_state_id");
                entity.Property(e => e.Notes).HasColumnName("notes");
                MapAuditColumns(entity);
            });

            modelBuilder.Entity<InterventionPlan>(entity =>
            {
                entity.ToTable("intervention_plans", CanonicalSchema);
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ResidentCaseId).HasColumnName("resident_case_id");
                entity.Property(e => e.EffectiveFrom).HasColumnName("effective_from");
                entity.Property(e => e.EffectiveTo).HasColumnName("effective_to");
                entity.Property(e => e.StatusStateId).HasColumnName("status_state_id");
                entity.Property(e => e.PlanDetails).HasColumnName("plan_details");
                MapAuditColumns(entity);
            });

            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.ToTable("user_profiles", CanonicalSchema);
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ExternalId).HasColumnName("external_id");
                entity.Property(e => e.DisplayName).HasColumnName("display_name");
                entity.Property(e => e.Email).HasColumnName("email");
                MapAuditColumns(entity);
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("user_roles", CanonicalSchema);
                entity.HasKey(e => new { e.UserProfileId, e.RoleId });

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserProfileId).HasColumnName("user_profile_id");
                entity.Property(e => e.RoleId).HasColumnName("role_id");
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("roles", CanonicalSchema);
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name).HasColumnName("name");
                MapAuditColumns(entity);
            });

            modelBuilder.Entity<StatusState>(entity =>
            {
                entity.ToTable("status_state", CanonicalSchema);
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Domain).HasColumnName("domain");
                entity.Property(e => e.Code).HasColumnName("code");
                entity.Property(e => e.Name).HasColumnName("name");
                MapAuditColumns(entity);
            });

            modelBuilder.Entity<CaseCategory>(entity =>
            {
                entity.ToTable("case_category", CanonicalSchema);
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Code).HasColumnName("code");
                entity.Property(e => e.Name).HasColumnName("name");
                MapAuditColumns(entity);
            });

            modelBuilder.Entity<CaseSubcategory>(entity =>
            {
                entity.ToTable("case_subcategory", CanonicalSchema);
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.CaseCategoryId).HasColumnName("case_category_id");
                entity.Property(e => e.Code).HasColumnName("code");
                entity.Property(e => e.Name).HasColumnName("name");
                MapAuditColumns(entity);
            });

            modelBuilder.Entity<ContributionType>(entity =>
            {
                entity.ToTable("contribution_type", CanonicalSchema);
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Code).HasColumnName("code");
                entity.Property(e => e.Name).HasColumnName("name");
                MapAuditColumns(entity);
            });

            modelBuilder.Entity<VisitType>(entity =>
            {
                entity.ToTable("visit_type", CanonicalSchema);
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Code).HasColumnName("code");
                entity.Property(e => e.Name).HasColumnName("name");
                MapAuditColumns(entity);
            });

            // Resolve SQL Server-style multiple cascade-path issues in providers that enforce stricter path rules.
            modelBuilder.Entity<CaseConference>()
                .HasOne(cc => cc.StatusState)
                .WithMany()
                .HasForeignKey(cc => cc.StatusStateId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<HomeVisit>()
                .HasOne(hv => hv.StatusState)
                .WithMany()
                .HasForeignKey(hv => hv.StatusStateId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<InterventionPlan>()
                .HasOne(ip => ip.StatusState)
                .WithMany()
                .HasForeignKey(ip => ip.StatusStateId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ResidentAssessment>()
                .HasOne(ra => ra.StatusState)
                .WithMany()
                .HasForeignKey(ra => ra.StatusStateId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<StatusState>().HasData(
                new StatusState { Id = 1, Name = "Active", Code = "active" },
                new StatusState { Id = 2, Name = "Pending", Code = "pending" },
                new StatusState { Id = 3, Name = "Closed", Code = "closed" }
            );
        }

        private static void MapAuditColumns<TEntity>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> entity)
            where TEntity : AuditableEntity
        {
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
        }
    }
}


