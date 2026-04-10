using Microsoft.EntityFrameworkCore;
using SafeHarbor.Data;
using SafeHarbor.DTOs;
using SafeHarbor.Infrastructure;
using SafeHarbor.Models;
using SafeHarbor.Models.Entities;
using SafeHarbor.Services.DonorImpact;

namespace SafeHarbor.Services;

public sealed class DbResidentRepository(SafeHarborDbContext db) : IResidentRepository
{
    public async Task<IReadOnlyList<Resident>> ListAsync(CancellationToken ct) =>
        await db.Residents.AsNoTracking().OrderBy(x => x.CreatedAtUtc).ToListAsync(ct);

    public Task<Resident?> FindAsync(Guid id, CancellationToken ct) =>
        db.Residents.FirstOrDefaultAsync(x => x.Id == id, ct)!;

    public async Task<Resident> CreateAsync(Resident resident, CancellationToken ct)
    {
        db.Residents.Add(resident);
        await db.SaveChangesAsync(ct);
        return resident;
    }

    public async Task<Resident?> UpdateAsync(Resident resident, CancellationToken ct)
    {
        var existing = await db.Residents.FirstOrDefaultAsync(x => x.Id == resident.Id, ct);
        if (existing is null) return null;

        existing.FullName = resident.FullName;
        existing.DateOfBirth = resident.DateOfBirth;
        existing.CaseWorkerEmail = resident.CaseWorkerEmail;
        existing.MedicalNotes = resident.MedicalNotes;
        existing.UpdatedAtUtc = resident.UpdatedAtUtc;

        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var existing = await db.Residents.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (existing is null) return false;

        db.Residents.Remove(existing);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public sealed class DbDonorRepository(SafeHarborDbContext db) : IDonorRepository
{
    public async Task<IReadOnlyList<Donor>> ListAsync(CancellationToken ct) =>
        await db.Donors.AsNoTracking().OrderBy(x => x.CreatedAtUtc).ToListAsync(ct);

    public Task<Donor?> FindAsync(Guid id, CancellationToken ct) =>
        db.Donors.FirstOrDefaultAsync(x => x.Id == id, ct)!;

    public Task<Donor?> FindByEmailAsync(string email, CancellationToken ct) =>
        db.Donors.FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower(), ct)!;

    public async Task<Donor> CreateAsync(Donor donor, CancellationToken ct)
    {
        db.Donors.Add(donor);
        await db.SaveChangesAsync(ct);
        return donor;
    }

    public async Task<Donor?> UpdateAsync(Donor donor, CancellationToken ct)
    {
        var existing = await db.Donors.FirstOrDefaultAsync(x => x.Id == donor.Id, ct);
        if (existing is null) return null;

        existing.DisplayName = donor.DisplayName;
        existing.Email = donor.Email;
        existing.LifetimeDonations = donor.LifetimeDonations;
        existing.PaymentToken = donor.PaymentToken;
        existing.UpdatedAtUtc = donor.UpdatedAtUtc;

        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var existing = await db.Donors.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (existing is null) return false;

        db.Donors.Remove(existing);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public sealed class DbCampaignRepository(SafeHarborDbContext db) : ICampaignRepository
{
    private const int ActiveCampaignStatusId = 1;

    public async Task<IReadOnlyList<Campaign>> ListAsync(CancellationToken ct) =>
        await db.Campaigns.AsNoTracking().ToListAsync(ct);

    public Task<Campaign?> GetActiveAsync(CancellationToken ct) =>
        db.Campaigns.AsNoTracking().FirstOrDefaultAsync(c => c.StatusStateId == ActiveCampaignStatusId, ct)!;
}

public sealed class DbContributionRepository(SafeHarborDbContext db) : IContributionRepository
{
    private const int CompletedContributionStatusId = 1;

    public async Task<IReadOnlyList<Contribution>> ListCompletedAsync(CancellationToken ct) =>
        await db.Contributions.AsNoTracking()
            .Where(c => c.StatusStateId == CompletedContributionStatusId)
            .OrderBy(c => c.ContributionDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Contribution>> ListCompletedByDonorAsync(Guid donorId, CancellationToken ct) =>
        await db.Contributions.AsNoTracking()
            .Where(c => c.DonorId == donorId && c.StatusStateId == CompletedContributionStatusId)
            .OrderBy(c => c.ContributionDate)
            .ToListAsync(ct);

    public async Task<Contribution> AddAsync(Contribution contribution, CancellationToken ct)
    {
        db.Contributions.Add(contribution);
        await db.SaveChangesAsync(ct);
        return contribution;
    }
}

// In-memory implementations are intentionally isolated behind an explicit development feature flag
// so deployed environments always use database-backed persistence.
public sealed class InMemoryResidentRepository(InMemoryDataStore store) : IResidentRepository
{
    public Task<IReadOnlyList<Resident>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Resident>>(store.Residents.ToList());
    public Task<Resident?> FindAsync(Guid id, CancellationToken ct) => Task.FromResult(store.Residents.FirstOrDefault(x => x.Id == id));
    public Task<Resident> CreateAsync(Resident resident, CancellationToken ct) { store.Residents.Add(resident); return Task.FromResult(resident); }
    public Task<Resident?> UpdateAsync(Resident resident, CancellationToken ct)
    {
        var existing = store.Residents.FirstOrDefault(x => x.Id == resident.Id);
        if (existing is null) return Task.FromResult<Resident?>(null);
        existing.FullName = resident.FullName;
        existing.DateOfBirth = resident.DateOfBirth;
        existing.CaseWorkerEmail = resident.CaseWorkerEmail;
        existing.MedicalNotes = resident.MedicalNotes;
        existing.UpdatedAtUtc = resident.UpdatedAtUtc;
        return Task.FromResult<Resident?>(existing);
    }
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var existing = store.Residents.FirstOrDefault(x => x.Id == id);
        if (existing is null) return Task.FromResult(false);
        store.Residents.Remove(existing);
        return Task.FromResult(true);
    }
}

public sealed class InMemoryDonorRepository(InMemoryDataStore store) : IDonorRepository
{
    public Task<IReadOnlyList<Donor>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Donor>>(store.Donors.ToList());
    public Task<Donor?> FindAsync(Guid id, CancellationToken ct) => Task.FromResult(store.Donors.FirstOrDefault(x => x.Id == id));
    public Task<Donor?> FindByEmailAsync(string email, CancellationToken ct) => Task.FromResult(store.Donors.FirstOrDefault(x => string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase)));
    public Task<Donor> CreateAsync(Donor donor, CancellationToken ct) { store.Donors.Add(donor); return Task.FromResult(donor); }
    public Task<Donor?> UpdateAsync(Donor donor, CancellationToken ct)
    {
        var existing = store.Donors.FirstOrDefault(x => x.Id == donor.Id);
        if (existing is null) return Task.FromResult<Donor?>(null);
        existing.DisplayName = donor.DisplayName;
        existing.Email = donor.Email;
        existing.LifetimeDonations = donor.LifetimeDonations;
        existing.PaymentToken = donor.PaymentToken;
        existing.UpdatedAtUtc = donor.UpdatedAtUtc;
        return Task.FromResult<Donor?>(existing);
    }
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var existing = store.Donors.FirstOrDefault(x => x.Id == id);
        if (existing is null) return Task.FromResult(false);
        store.Donors.Remove(existing);
        return Task.FromResult(true);
    }
}

public sealed class InMemoryCampaignRepository(InMemoryDataStore store) : ICampaignRepository
{
    private const int ActiveCampaignStatusId = 1;
    public Task<IReadOnlyList<Campaign>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Campaign>>(store.Campaigns.ToList());
    public Task<Campaign?> GetActiveAsync(CancellationToken ct) => Task.FromResult(store.Campaigns.FirstOrDefault(c => c.StatusStateId == ActiveCampaignStatusId));
}

public sealed class InMemoryContributionRepository(InMemoryDataStore store) : IContributionRepository
{
    private const int CompletedContributionStatusId = 1;
    public Task<IReadOnlyList<Contribution>> ListCompletedAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Contribution>>(store.Contributions.Where(c => c.StatusStateId == CompletedContributionStatusId).OrderBy(c => c.ContributionDate).ToList());
    public Task<IReadOnlyList<Contribution>> ListCompletedByDonorAsync(Guid donorId, CancellationToken ct) => Task.FromResult<IReadOnlyList<Contribution>>(store.Contributions.Where(c => c.DonorId == donorId && c.StatusStateId == CompletedContributionStatusId).OrderBy(c => c.ContributionDate).ToList());
    public Task<Contribution> AddAsync(Contribution contribution, CancellationToken ct) { store.Contributions.Add(contribution); return Task.FromResult(contribution); }
}

public sealed class ResidentAdminService(
    IResidentRepository residents,
    IAuditLogger auditLogger,
    IDataRetentionRedactionService retentionRedactionService) : IResidentAdminService
{
    public async Task<IReadOnlyCollection<ResidentAdminResponse>> GetAllAsync(CancellationToken ct) =>
        (await residents.ListAsync(ct)).Select(MapAdmin).ToArray();

    public async Task<ResidentAdminResponse?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var resident = await residents.FindAsync(id, ct);
        return resident is null ? null : MapAdmin(resident);
    }

    public async Task<ResidentAdminResponse> CreateAsync(ResidentCreateRequest request, string actor, CancellationToken ct)
    {
        var resident = new Resident
        {
            FullName = request.FullName,
            DateOfBirth = request.DateOfBirth,
            CaseWorkerEmail = request.CaseWorkerEmail,
            MedicalNotes = request.MedicalNotes ?? string.Empty,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        resident = await residents.CreateAsync(resident, ct);
        auditLogger.RecordMutation("resident", "create", resident.Id, actor);
        return MapAdmin(resident);
    }

    public async Task<ResidentAdminResponse?> UpdateAsync(Guid id, ResidentUpdateRequest request, string actor, CancellationToken ct)
    {
        var updated = await residents.UpdateAsync(new Resident
        {
            Id = id,
            FullName = request.FullName,
            DateOfBirth = request.DateOfBirth,
            CaseWorkerEmail = request.CaseWorkerEmail,
            MedicalNotes = request.MedicalNotes ?? string.Empty,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        }, ct);

        if (updated is null) return null;
        auditLogger.RecordMutation("resident", "update", updated.Id, actor);
        return MapAdmin(updated);
    }

    public async Task<bool> DeleteAsync(Guid id, string actor, CancellationToken ct)
    {
        var deleted = await residents.DeleteAsync(id, ct);
        if (deleted) auditLogger.RecordMutation("resident", "delete", id, actor);
        return deleted;
    }

    public async Task<IReadOnlyCollection<ResidentAdminResponse>> ExportSnapshotAsync(CancellationToken ct)
    {
        var payload = (await residents.ListAsync(ct))
            .Select(x => MapAdmin(x) with { MedicalNotes = retentionRedactionService.RedactFreeText(x.MedicalNotes) })
            .ToArray();

        return retentionRedactionService.ApplyRetentionPolicy(payload, "resident_export");
    }

    private static ResidentAdminResponse MapAdmin(Resident resident) =>
        new(resident.Id, resident.FullName, resident.DateOfBirth, resident.CaseWorkerEmail, resident.MedicalNotes, resident.CreatedAtUtc, resident.UpdatedAtUtc);
}

public sealed class DonorAdminService(
    IDonorRepository donors,
    IContributionRepository contributions,
    IAuditLogger auditLogger,
    IDataRetentionRedactionService retentionRedactionService) : IDonorAdminService
{
    public async Task<IReadOnlyCollection<DonorAdminResponse>> GetAllAsync(CancellationToken ct)
    {
        var allDonors  = await donors.ListAsync(ct);
        var allContribs = await contributions.ListCompletedAsync(ct);
        var byDonor    = allContribs
            .GroupBy(c => c.DonorId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return allDonors.Select(d =>
        {
            var dc = byDonor.GetValueOrDefault(d.Id);
            return MapAdmin(d) with
            {
                ContributionCount     = dc?.Count ?? 0,
                LastContributionDate  = dc?.Max(c => c.ContributionDate),
                FirstContributionDate = dc?.Min(c => c.ContributionDate),
                UniqueChannels        = dc?.Select(c => c.ContributionTypeId).Distinct().Count()
            };
        }).ToArray();
    }

    public async Task<DonorAdminResponse?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var donor = await donors.FindAsync(id, ct);
        return donor is null ? null : MapAdmin(donor);
    }

    public async Task<DonorAdminResponse> CreateAsync(DonorCreateRequest request, string actor, CancellationToken ct)
    {
        var donor = new Donor
        {
            DisplayName = request.DisplayName,
            Email = request.Email,
            LifetimeDonations = request.LifetimeDonations,
            PaymentToken = request.PaymentToken,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        donor = await donors.CreateAsync(donor, ct);
        auditLogger.RecordMutation("donor", "create", donor.Id, actor);
        return MapAdmin(donor);
    }

    public async Task<DonorAdminResponse?> UpdateAsync(Guid id, DonorUpdateRequest request, string actor, CancellationToken ct)
    {
        var updated = await donors.UpdateAsync(new Donor
        {
            Id = id,
            DisplayName = request.DisplayName,
            Email = request.Email,
            LifetimeDonations = request.LifetimeDonations,
            PaymentToken = request.PaymentToken,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        }, ct);

        if (updated is null) return null;
        auditLogger.RecordMutation("donor", "update", updated.Id, actor);
        return MapAdmin(updated);
    }

    public async Task<bool> DeleteAsync(Guid id, string actor, CancellationToken ct)
    {
        var deleted = await donors.DeleteAsync(id, ct);
        if (deleted) auditLogger.RecordMutation("donor", "delete", id, actor);
        return deleted;
    }

    public async Task<IReadOnlyCollection<DonorPublicResponse>> ReportSummaryAsync(CancellationToken ct)
    {
        var payload = (await donors.ListAsync(ct))
            .Select(x => new DonorPublicResponse(x.Id, x.DisplayName, x.LifetimeDonations))
            .ToArray();

        return retentionRedactionService.ApplyRetentionPolicy(payload, "donor_summary_report");
    }

    private static DonorAdminResponse MapAdmin(Donor donor) =>
        new(donor.Id, donor.DisplayName, donor.Email, donor.LifetimeDonations, donor.PaymentToken ?? string.Empty, donor.CreatedAtUtc, donor.UpdatedAtUtc);
}

public sealed class PublicRecordsService(IResidentRepository residents, IDonorRepository donors) : IPublicRecordsService
{
    public async Task<IReadOnlyCollection<ResidentPublicResponse>> GetResidentsAsync(CancellationToken ct) =>
        (await residents.ListAsync(ct))
            .Select(r => new ResidentPublicResponse(r.Id, r.FullName, CalculateAgeYears(r.DateOfBirth)))
            .ToArray();

    public async Task<IReadOnlyCollection<DonorPublicResponse>> GetDonorsAsync(CancellationToken ct) =>
        (await donors.ListAsync(ct)).Select(d => new DonorPublicResponse(d.Id, d.DisplayName, d.LifetimeDonations)).ToArray();

    private static int CalculateAgeYears(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var years = today.Year - dateOfBirth.Year;
        if (today < dateOfBirth.AddYears(years)) years--;
        return years;
    }
}

public sealed class DonorDashboardService(
    IDonorRepository donorRepository,
    IContributionRepository contributionRepository,
    ICampaignRepository campaignRepository,
    IDonorImpactCalculator impactCalculator,
    ILogger<DonorDashboardService> logger) : IDonorDashboardService
{
    private const int CompletedContributionStatusId = 1;
    private const int OnlineDonationTypeId = 1;

    public async Task<DonorDashboardResponse?> GetDashboardAsync(Guid? donorId, string? email, CancellationToken ct)
    {
        var donor = await ResolveDonorAsync(donorId, email, ct);
        if (donor is null) return null;

        var donorContributions = await contributionRepository.ListCompletedByDonorAsync(donor.Id, ct);
        var lifetimeDonated = donorContributions.Sum(c => c.Amount);
        var monthlyHistory = BuildMonthlyHistory(donorContributions);
        var campaignSummary = await BuildCampaignGoalSummaryAsync(donor.Id, ct);

        var impact = impactCalculator.Calculate(lifetimeDonated);
        var impactSummary = new DonorImpactSummary(impact.GirlsHelped, impact.ImpactLabel, impact.ModelVersion);

        return new DonorDashboardResponse(donor.DisplayName, lifetimeDonated, monthlyHistory, campaignSummary, impactSummary);
    }

    public async Task<NewContributionResponse?> AddContributionAsync(Guid? donorId, string? email, NewContributionRequest request, CancellationToken ct)
    {
        var donor = await ResolveDonorAsync(donorId, email, ct);
        if (donor is null) return null;

        var activeCampaign = await campaignRepository.GetActiveAsync(ct);
        var contribution = new Contribution
        {
            Id = Guid.NewGuid(),
            DonorId = donor.Id,
            CampaignId = request.CampaignId ?? activeCampaign?.Id,
            Amount = request.Amount,
            ContributionDate = DateTimeOffset.UtcNow,
            ContributionTypeId = OnlineDonationTypeId,
            StatusStateId = CompletedContributionStatusId,
        };

        await contributionRepository.AddAsync(contribution, ct);

        donor.LifetimeDonations += request.Amount;
        donor.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await donorRepository.UpdateAsync(donor, ct);

        return new NewContributionResponse(contribution.Id, "Thank you! Your donation has been added.");
    }

    private async Task<Donor?> ResolveDonorAsync(Guid? donorId, string? email, CancellationToken ct)
    {
        if (donorId is { } id)
        {
            var donorById = await donorRepository.FindAsync(id, ct);
            if (donorById is not null) return donorById;

            // NOTE: Log includes stable identifiers so operations can quickly backfill missing
            // domain profiles through the auth-maintenance reconciliation endpoint.
            logger.LogWarning(
                "Donor dashboard identity mismatch: no donor profile for claim oid {DonorId}. Email claim: {Email}.",
                id,
                email ?? "<null>");
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var donorByEmail = await donorRepository.FindByEmailAsync(email, ct);
            if (donorByEmail is not null)
            {
                return donorByEmail;
            }

            logger.LogWarning(
                "Donor dashboard identity mismatch: no donor profile for email {Email}. Suggested action: run /api/admin/auth-maintenance/reconcile-domain-profiles.",
                email);
        }

        return null;
    }

    private static IReadOnlyList<MonthlyDonationPoint> BuildMonthlyHistory(IEnumerable<Contribution> contributions)
    {
        var grouped = contributions
            .GroupBy(c => c.ContributionDate.ToString("yyyy-MM"))
            .ToDictionary(g => g.Key, g => g.Sum(c => c.Amount));

        var result = new List<MonthlyDonationPoint>(12);
        var reference = DateTimeOffset.UtcNow;

        for (int i = 11; i >= 0; i--)
        {
            var month = reference.AddMonths(-i);
            var key = month.ToString("yyyy-MM");
            result.Add(new MonthlyDonationPoint(key, grouped.TryGetValue(key, out var amount) ? amount : 0m));
        }

        return result;
    }

    private async Task<CampaignGoalSummary?> BuildCampaignGoalSummaryAsync(Guid donorId, CancellationToken ct)
    {
        var activeCampaign = await campaignRepository.GetActiveAsync(ct);
        if (activeCampaign is null) return null;

        var completedContributions = await contributionRepository.ListCompletedAsync(ct);
        var campaignCompleted = completedContributions.Where(c => c.CampaignId == activeCampaign.Id).ToList();

        var totalRaisedAllDonors = campaignCompleted.Sum(c => c.Amount);
        var thisDonorContributed = campaignCompleted.Where(c => c.DonorId == donorId).Sum(c => c.Amount);

        var progressPercent = activeCampaign.GoalAmount > 0
            ? Math.Min(100m, totalRaisedAllDonors / activeCampaign.GoalAmount * 100m)
            : 0m;

        return new CampaignGoalSummary(activeCampaign.Id, activeCampaign.Name, activeCampaign.GoalAmount, totalRaisedAllDonors, thisDonorContributed, Math.Round(progressPercent, 1));
    }
}

public sealed class DonorAnalyticsService(
    IDonorRepository donorRepository,
    IContributionRepository contributionRepository,
    ICampaignRepository campaignRepository) : IDonorAnalyticsService
{
    private const int ActiveWindowDays = 90;
    private const int TopDonorLimit = 5;

    public async Task<DonorAnalyticsResponse> GetAnalyticsAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var donors = await donorRepository.ListAsync(ct);
        var completedContributions = await contributionRepository.ListCompletedAsync(ct);

        var totalDonationsReceived = completedContributions.Sum(c => c.Amount);
        var totalContributionCount = completedContributions.Count;
        var totalDonorCount = donors.Count;

        var activeCutoff = now.AddDays(-ActiveWindowDays);
        var activeDonorCount = completedContributions
            .Where(c => c.ContributionDate >= activeCutoff)
            .Select(c => c.DonorId)
            .Distinct()
            .Count();

        var retentionRate = 0m;
        if (totalDonorCount > 0)
        {
            var repeatDonorCount = completedContributions
                .GroupBy(c => c.DonorId)
                .Count(g => g.Count() >= 2);
            retentionRate = Math.Round((decimal)repeatDonorCount / totalDonorCount * 100, 1);
        }

        var averageGiftSize = totalContributionCount > 0
            ? Math.Round(totalDonationsReceived / totalContributionCount, 2)
            : 0m;

        var monthlyTrend = BuildMonthlyTrend(completedContributions, now);
        var campaigns = await BuildCampaignSummariesAsync(completedContributions, ct);
        var topDonors = BuildTopDonors(completedContributions, donors);

        return new DonorAnalyticsResponse(
            totalDonationsReceived,
            totalDonorCount,
            activeDonorCount,
            retentionRate,
            averageGiftSize,
            totalContributionCount,
            monthlyTrend,
            campaigns,
            topDonors);
    }

    private static IReadOnlyList<AnalyticsMonthlyPoint> BuildMonthlyTrend(IReadOnlyList<Contribution> contributions, DateTimeOffset reference)
    {
        var amountByMonth = contributions
            .GroupBy(c => c.ContributionDate.ToString("yyyy-MM"))
            .ToDictionary(g => g.Key, g => g.Sum(c => c.Amount));

        var firstMonthPerDonor = contributions
            .GroupBy(c => c.DonorId)
            .ToDictionary(g => g.Key, g => g.Min(c => c.ContributionDate).ToString("yyyy-MM"));

        var newDonorsByMonth = firstMonthPerDonor.Values
            .GroupBy(m => m)
            .ToDictionary(g => g.Key, g => g.Count());

        var result = new List<AnalyticsMonthlyPoint>(12);
        for (int i = 11; i >= 0; i--)
        {
            var month = reference.AddMonths(-i);
            var key = month.ToString("yyyy-MM");
            result.Add(new AnalyticsMonthlyPoint(
                key,
                amountByMonth.TryGetValue(key, out var amount) ? amount : 0m,
                newDonorsByMonth.TryGetValue(key, out var newDonors) ? newDonors : 0));
        }

        return result;
    }

    private async Task<IReadOnlyList<CampaignAnalyticsSummary>> BuildCampaignSummariesAsync(IReadOnlyList<Contribution> contributions, CancellationToken ct)
    {
        var campaigns = await campaignRepository.ListAsync(ct);

        return campaigns
            .Select(campaign =>
            {
                var campaignContributions = contributions.Where(c => c.CampaignId == campaign.Id).ToList();
                var totalRaised = campaignContributions.Sum(c => c.Amount);
                var donorCount = campaignContributions.Select(c => c.DonorId).Distinct().Count();
                var progressPercent = campaign.GoalAmount > 0
                    ? Math.Min(100m, Math.Round(totalRaised / campaign.GoalAmount * 100, 1))
                    : 0m;

                return new CampaignAnalyticsSummary(campaign.Id, campaign.Name, campaign.GoalAmount, totalRaised, progressPercent, donorCount, campaignContributions.Count);
            })
            .OrderByDescending(c => c.TotalRaised)
            .ToList();
    }

    private static IReadOnlyList<TopDonorSummary> BuildTopDonors(IReadOnlyList<Contribution> contributions, IReadOnlyList<Donor> donors)
    {
        return contributions
            .GroupBy(c => c.DonorId)
            .Select(g =>
            {
                var donor = donors.FirstOrDefault(d => d.Id == g.Key);
                return new
                {
                    DisplayName = donor?.DisplayName ?? "Unknown Donor",
                    LifetimeDonated = g.Sum(c => c.Amount),
                    ContributionCount = g.Count(),
                };
            })
            .OrderByDescending(d => d.LifetimeDonated)
            .Take(TopDonorLimit)
            .Select(d => new TopDonorSummary(d.DisplayName, d.LifetimeDonated, d.ContributionCount))
            .ToList();
    }
}
