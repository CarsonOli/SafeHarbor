using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SafeHarbor.Data;
using SafeHarbor.Models.Entities;
using SafeHarbor.Models.Lookups;

namespace SafeHarbor.Tests;

/// <summary>
/// Test host factory that swaps production JWT auth for deterministic header-driven auth.
/// Authorization policies still run unchanged, so role gates are exercised end-to-end.
/// </summary>
public sealed class SafeHarborApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IConfigureOptions<AuthenticationOptions>>();
            services.RemoveAll<IPostConfigureOptions<AuthenticationOptions>>();
            services.RemoveAll<DbContextOptions<SafeHarborDbContext>>();

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                    options.DefaultChallengeScheme = TestAuthHandler.Scheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.Scheme,
                    _ => { });

            // Integration tests need deterministic in-memory persistence so module endpoints
            // can perform real CRUD/query logic without requiring an external PostgreSQL server.
            services.AddDbContext<SafeHarborDbContext>(options =>
                options.UseInMemoryDatabase($"safeharbor-tests-{Guid.NewGuid()}"));

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SafeHarborDbContext>();
            SeedDatabase(db);
        });
    }

    private static void SeedDatabase(SafeHarborDbContext db)
    {
        db.Database.EnsureCreated();

        var safehouseId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var residentCaseId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var donorId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var contributionId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        if (!db.Safehouses.Any())
        {
            db.Safehouses.Add(new Safehouse { Id = safehouseId, Name = "Main Safehouse", Region = "Metro" });
        }

        if (!db.Set<CaseCategory>().Any())
        {
            db.Set<CaseCategory>().Add(new CaseCategory { Id = 1, Code = "SRV", Name = "Service" });
        }

        if (!db.Set<VisitType>().Any())
        {
            db.Set<VisitType>().Add(new VisitType { Id = 1, Code = "HOME", Name = "Home" });
        }

        if (!db.StatusState.Any())
        {
            db.StatusState.AddRange(
                new StatusState { Id = 1, Name = "Active", Code = "ACTIVE" },
                new StatusState { Id = 2, Name = "Pending", Code = "PENDING" },
                new StatusState { Id = 3, Name = "Closed", Code = "CLOSED" });
        }

        if (!db.ResidentCases.Any())
        {
            db.ResidentCases.Add(new ResidentCase
            {
                Id = residentCaseId,
                SafehouseId = safehouseId,
                CaseCategoryId = 1,
                StatusStateId = 1,
                OpenedAt = DateTimeOffset.UtcNow.AddDays(-7)
            });
        }

        if (!db.ProcessRecordings.Any())
        {
            db.ProcessRecordings.Add(new ProcessRecording
            {
                Id = Guid.NewGuid(),
                ResidentCaseId = residentCaseId,
                RecordedAt = DateTimeOffset.UtcNow.AddDays(-1),
                Summary = "Initial process note"
            });
        }

        if (!db.HomeVisits.Any())
        {
            db.HomeVisits.Add(new HomeVisit
            {
                Id = Guid.NewGuid(),
                ResidentCaseId = residentCaseId,
                VisitTypeId = 1,
                StatusStateId = 1,
                VisitDate = DateTimeOffset.UtcNow.AddDays(-2),
                Notes = "Routine check"
            });
        }

        if (!db.CaseConferences.Any())
        {
            db.CaseConferences.AddRange(
                new CaseConference
                {
                    Id = Guid.NewGuid(),
                    ResidentCaseId = residentCaseId,
                    StatusStateId = 1,
                    ConferenceDate = DateTimeOffset.UtcNow.AddDays(2),
                    OutcomeSummary = "Upcoming"
                },
                new CaseConference
                {
                    Id = Guid.NewGuid(),
                    ResidentCaseId = residentCaseId,
                    StatusStateId = 3,
                    ConferenceDate = DateTimeOffset.UtcNow.AddDays(-10),
                    OutcomeSummary = "Completed"
                });
        }

        if (!db.Donors.Any())
        {
            db.Donors.Add(new Donor
            {
                Id = donorId,
                Name = "Donor One",
                DisplayName = "Donor One",
                Email = "donor1@example.com",
                LastActivityAt = DateTimeOffset.UtcNow,
                LifetimeDonations = 100m
            });
        }

        if (!db.Contributions.Any())
        {
            db.Contributions.Add(new Contribution
            {
                Id = contributionId,
                DonorId = donorId,
                Amount = 100m,
                ContributionTypeId = 1,
                StatusStateId = 1,
                ContributionDate = DateTimeOffset.UtcNow.AddDays(-3)
            });
        }

        db.SaveChanges();
    }
}
