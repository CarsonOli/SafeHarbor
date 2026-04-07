using Microsoft.EntityFrameworkCore;
using SafeHarbor.Models.Entities;
using SafeHarbor.Models.Lookups;
using SafeHarbor.Models;

namespace SafeHarbor.Data
{
    public class SafeHarborDbContext : DbContext
    {
        public SafeHarborDbContext(DbContextOptions<SafeHarborDbContext> options)
            : base(options)
        {
        }

        // --- Identity & Roles (Aligned with Instructions) ---
        public DbSet<UserProfile> UserProfiles { get; set; } 
        public DbSet<Role> Roles { get; set; }          
        public DbSet<UserRole> UserRoles { get; set; }
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
        public DbSet<SocialPostMetric> SocialPostMetrics { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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

            // 2. Composite Key for UserRole
            modelBuilder.Entity<UserRole>()
                .HasKey(sr => new { sr.UserProfileId, sr.RoleId });
            
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
            
            modelBuilder.Entity<Role>().HasData(
                new Role { 
                    Id = Guid.Parse("d2b2f671-5b1a-4a2b-8c2e-4b6a8f1d2c34"), 
                    Name = "Admin" 
                },
                new Role { 
                    Id = Guid.Parse("a1c3e5d7-2f4b-4e6a-8d0c-2b4a6f8d0e2c"), 
                    Name = "Staff" 
                }
            );

            modelBuilder.Entity<StatusState>().HasData(
                new StatusState { Id = 1, Name = "Active" },
                new StatusState { Id = 2, Name = "Pending" },
                new StatusState { Id = 3, Name = "Closed" }
            );
        }
    }
}