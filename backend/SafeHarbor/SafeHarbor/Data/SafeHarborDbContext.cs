using Microsoft.EntityFrameworkCore;
using SafeHarbor.Models.Entities;
using SafeHarbor.Models.Lookups;

namespace SafeHarbor.Data
{
    public class SafeHarborDbContext : DbContext
    {
        public SafeHarborDbContext(DbContextOptions<SafeHarborDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<StatusState> StatusState { get; set; }

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
        public DbSet<Donor> Donors { get; set; }
        public DbSet<Campaign> Campaigns { get; set; }
        public DbSet<Contribution> Contributions { get; set; }
        public DbSet<ContributionAllocation> ContributionAllocations { get; set; }
        public DbSet<SocialPostMetric> SocialPostMetrics { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Keep user auth data mapped to the existing Postgres table/column names.
            // We scope the explicit schema mapping to this entity so existing table mappings stay unchanged.
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users", "lighthouse");
                entity.HasKey(u => u.UserId);

                entity.Property(u => u.UserId).HasColumnName("user_id");
                entity.Property(u => u.FirstName).HasColumnName("f_name");
                entity.Property(u => u.LastName).HasColumnName("l_name");
                entity.Property(u => u.Email).HasColumnName("email");
                entity.Property(u => u.Role).HasColumnName("role");
                entity.Property(u => u.PasswordHash).HasColumnName("password_hash");
                entity.Property(u => u.CreatedAt).HasColumnName("created_at");
                entity.Property(u => u.UpdatedAt).HasColumnName("updated_at");
            });

            // 1. Fix Decimal Precision (Stops the truncation warnings)
            modelBuilder.Entity<Contribution>()
                .Property(c => c.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Donor>()
                .Property(d => d.LifetimeDonations)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SocialPostMetric>()
                .Property(s => s.AttributedDonationAmount)
                .HasPrecision(18, 2);

            // This line stops the warning you saw in your logs earlier:
            modelBuilder.Entity<ContributionAllocation>()
                .Property(ca => ca.AmountAllocated)
                .HasPrecision(18, 2);

            // 3. Resolve the SQL Server Cascade Path Error
            modelBuilder.Entity<CaseConference>()
                .HasOne(cc => cc.StatusState)
                .WithMany()
                .HasForeignKey(cc => cc.StatusStateId)
                .OnDelete(DeleteBehavior.NoAction);

            // Fix for the new error on HomeVisits
            modelBuilder.Entity<HomeVisit>()
                .HasOne(hv => hv.StatusState)
                .WithMany()
                .HasForeignKey(hv => hv.StatusStateId)
                .OnDelete(DeleteBehavior.NoAction);

            // Fix for InterventionPlans
            modelBuilder.Entity<InterventionPlan>()
                .HasOne(ip => ip.StatusState)
                .WithMany()
                .HasForeignKey(ip => ip.StatusStateId)
                .OnDelete(DeleteBehavior.NoAction);

            // Fix for ResidentAssessments (likely the next one to fail)
            modelBuilder.Entity<ResidentAssessment>()
                .HasOne(ra => ra.StatusState)
                .WithMany()
                .HasForeignKey(ra => ra.StatusStateId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<StatusState>().HasData(
                new StatusState { Id = 1, Name = "Active" },
                new StatusState { Id = 2, Name = "Pending" },
                new StatusState { Id = 3, Name = "Closed" }
            );
        }
    }
}
