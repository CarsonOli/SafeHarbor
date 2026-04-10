using Microsoft.EntityFrameworkCore;
using SafeHarbor.Data;
using SafeHarbor.DTOs;
using SafeHarbor.Models.Entities;
using SafeHarbor.Models.Enums;

namespace SafeHarbor.Services.Admin;

public interface ICaseloadInventoryService
{
    Task<PagedResult<ResidentCaseListItem>> GetResidentsAsync(PagingQuery query, CancellationToken ct);
    Task<CaseloadLookupsResponse> GetLookupsAsync(CancellationToken ct);
    Task<ResidentCaseListItem> CreateResidentCaseAsync(CreateResidentCaseRequest request, CancellationToken ct);
    Task<ResidentCaseListItem?> UpdateResidentCaseAsync(Guid id, UpdateResidentCaseRequest request, CancellationToken ct);
    Task<bool> DeleteResidentCaseAsync(Guid id, CancellationToken ct);
}

public interface IProcessRecordingService
{
    Task<PagedResult<ProcessRecordItem>> GetAsync(PagingQuery query, CancellationToken ct);
    Task<ProcessRecordItem> CreateAsync(CreateProcessRecordRequest request, CancellationToken ct);
    Task<ProcessRecordItem?> UpdateAsync(Guid id, CreateProcessRecordRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

public interface IVisitationConferenceService
{
    Task<PagedResult<HomeVisitItem>> GetVisitsAsync(PagingQuery query, CancellationToken ct);
    Task<PagedResult<CaseConferenceItem>> GetUpcomingAsync(PagingQuery query, CancellationToken ct);
    Task<PagedResult<CaseConferenceItem>> GetPreviousAsync(PagingQuery query, CancellationToken ct);
}

public interface IDonorContributionService
{
    Task<PagedResult<DonorListItem>> GetDonorsAsync(PagingQuery query, CancellationToken ct);
    Task<DonorListItem> CreateDonorAsync(CreateDonorRequest request, CancellationToken ct);
    Task<ContributionListItem> CreateContributionAsync(CreateContributionRequest request, CancellationToken ct);
    Task<bool> CreateAllocationAsync(CreateAllocationRequest request, CancellationToken ct);
}

public interface IReportsAnalyticsService
{
    Task<ReportsAnalyticsResponse> GetAsync(CancellationToken ct);
}

public sealed class CaseloadInventoryService(SafeHarborDbContext db) : ICaseloadInventoryService
{
    public async Task<PagedResult<ResidentCaseListItem>> GetResidentsAsync(PagingQuery query, CancellationToken ct)
    {
        var q = db.ResidentCases.AsNoTracking()
            .Include(x => x.Safehouse)
            .Include(x => x.CaseCategory)
            .Include(x => x.StatusState)
            .Include(x => x.Resident)
            .AsQueryable();

        if (query.SafehouseId is { } safehouseId)
        {
            q = q.Where(x => x.SafehouseId == safehouseId);
        }

        if (query.StatusStateId is { } statusStateId)
        {
            q = q.Where(x => x.StatusStateId == statusStateId);
        }

        if (query.CategoryId is { } categoryId)
        {
            q = q.Where(x => x.CaseCategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            q = q.Where(x =>
                (x.Safehouse != null && x.Safehouse.Name.ToLower().Contains(search)) ||
                (x.CaseCategory != null && x.CaseCategory.Name.ToLower().Contains(search)) ||
                (x.StatusState != null && x.StatusState.Name.ToLower().Contains(search)) ||
                (x.Resident != null && x.Resident.FullName.ToLower().Contains(search)));
        }

        q = query.Desc ? q.OrderByDescending(x => x.OpenedAt) : q.OrderBy(x => x.OpenedAt);

        var total = await q.CountAsync(ct);
        var page = query.NormalizedPage;
        var pageSize = query.NormalizedPageSize;

        var items = await q.Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ResidentCaseListItem(
                x.Id,
                x.SafehouseId,
                x.Safehouse != null ? x.Safehouse.Name : "Unknown",
                x.CaseCategoryId,
                x.CaseCategory != null ? x.CaseCategory.Name : "Unknown",
                x.StatusStateId,
                x.StatusState != null ? x.StatusState.Name : "Unknown",
                x.CreatedBy,
                x.Resident != null ? x.Resident.FullName : null,
                x.OpenedAt,
                x.ClosedAt))
            .ToArrayAsync(ct);

        return new PagedResult<ResidentCaseListItem>(items, page, pageSize, total);
    }

    public async Task<CaseloadLookupsResponse> GetLookupsAsync(CancellationToken ct)
    {
        var safehouses = await db.Safehouses.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new CaseloadSafehouseItem(x.Id.ToString(), x.Name))
            .ToArrayAsync(ct);

        var categories = await db.CaseCategories.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new CaseloadLookupItem(x.Id, x.Name))
            .ToArrayAsync(ct);

        var statuses = await db.StatusState.AsNoTracking()
            .Where(x => x.Domain == StatusDomain.ResidentCase)
            .OrderBy(x => x.Name)
            .Select(x => new CaseloadLookupItem(x.Id, x.Name))
            .ToArrayAsync(ct);

        return new CaseloadLookupsResponse(safehouses, categories, statuses);
    }

    public async Task<ResidentCaseListItem> CreateResidentCaseAsync(CreateResidentCaseRequest request, CancellationToken ct)
    {
        var entity = new ResidentCase
        {
            Id = Guid.NewGuid(),
            SafehouseId = request.SafehouseId,
            CaseCategoryId = request.CaseCategoryId,
            CaseSubcategoryId = request.CaseSubcategoryId,
            StatusStateId = request.StatusStateId,
            ResidentId = request.ResidentUserId,
            OpenedAt = request.OpenedAt ?? DateTimeOffset.UtcNow
        };

        db.ResidentCases.Add(entity);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<ResidentCaseListItem?> UpdateResidentCaseAsync(Guid id, UpdateResidentCaseRequest request, CancellationToken ct)
    {
        var entity = await db.ResidentCases.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return null;

        entity.SafehouseId = request.SafehouseId;
        entity.CaseCategoryId = request.CaseCategoryId;
        entity.CaseSubcategoryId = request.CaseSubcategoryId;
        entity.StatusStateId = request.StatusStateId;
        entity.ResidentId = request.ResidentUserId;
        entity.ClosedAt = request.ClosedAt;

        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<bool> DeleteResidentCaseAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.ResidentCases.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return false;

        db.ResidentCases.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task<ResidentCaseListItem> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await db.ResidentCases.AsNoTracking()
            .Include(x => x.Safehouse)
            .Include(x => x.CaseCategory)
            .Include(x => x.StatusState)
            .Where(x => x.Id == id)
            .Select(x => new ResidentCaseListItem(
                x.Id,
                x.SafehouseId,
                x.Safehouse != null ? x.Safehouse.Name : "Unknown",
                x.CaseCategoryId,
                x.CaseCategory != null ? x.CaseCategory.Name : "Unknown",
                x.StatusStateId,
                x.StatusState != null ? x.StatusState.Name : "Unknown",
                x.CreatedBy,
                x.Resident != null ? x.Resident.FullName : null,
                x.OpenedAt,
                x.ClosedAt))
            .FirstAsync(ct);
    }
}

public sealed class ProcessRecordingService(SafeHarborDbContext db) : IProcessRecordingService
{
    public async Task<PagedResult<ProcessRecordItem>> GetAsync(PagingQuery query, CancellationToken ct)
    {
        var q = db.ProcessRecordings.AsNoTracking().AsQueryable();

        if (query.ResidentCaseId is { } residentCaseId)
        {
            q = q.Where(x => x.ResidentCaseId == residentCaseId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            q = q.Where(x =>
                x.Summary.ToLower().Contains(search) ||
                x.SocialWorker.ToLower().Contains(search) ||
                x.EmotionalStateObserved.ToLower().Contains(search) ||
                (x.InterventionsApplied != null && x.InterventionsApplied.ToLower().Contains(search)));
        }

        q = query.Desc ? q.OrderByDescending(x => x.RecordedAt) : q.OrderBy(x => x.RecordedAt);

        var total = await q.CountAsync(ct);
        var page = query.NormalizedPage;
        var pageSize = query.NormalizedPageSize;

        var items = await q.Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ProcessRecordItem(
                x.Id,
                x.ResidentCaseId,
                x.RecordedAt,
                x.SocialWorker,
                x.SessionType,
                x.SessionDurationMinutes,
                x.EmotionalStateObserved,
                x.EmotionalStateEnd,
                x.Summary,
                x.InterventionsApplied,
                x.FollowUpActions,
                x.ProgressNoted,
                x.ConcernsFlagged,
                x.ReferralMade,
                x.NotesRestricted != null && x.NotesRestricted != string.Empty))
            .ToArrayAsync(ct);

        return new PagedResult<ProcessRecordItem>(items, page, pageSize, total);
    }

    public async Task<ProcessRecordItem> CreateAsync(CreateProcessRecordRequest request, CancellationToken ct)
    {
        var entity = new ProcessRecording
        {
            Id = Guid.NewGuid(),
            ResidentCaseId = request.ResidentCaseId,
            RecordedAt = request.RecordedAt ?? DateTimeOffset.UtcNow,
            SocialWorker = request.SocialWorker,
            SessionType = request.SessionType,
            SessionDurationMinutes = request.SessionDurationMinutes,
            EmotionalStateObserved = request.EmotionalStateObserved,
            EmotionalStateEnd = request.EmotionalStateEnd,
            Summary = request.Summary,
            InterventionsApplied = request.InterventionsApplied,
            FollowUpActions = request.FollowUpActions,
            ProgressNoted = request.ProgressNoted,
            ConcernsFlagged = request.ConcernsFlagged,
            ReferralMade = request.ReferralMade,
            NotesRestricted = request.NotesRestricted
        };

        db.ProcessRecordings.Add(entity);
        await db.SaveChangesAsync(ct);

        return new ProcessRecordItem(
            entity.Id, entity.ResidentCaseId, entity.RecordedAt,
            entity.SocialWorker, entity.SessionType, entity.SessionDurationMinutes,
            entity.EmotionalStateObserved, entity.EmotionalStateEnd, entity.Summary,
            entity.InterventionsApplied, entity.FollowUpActions,
            entity.ProgressNoted, entity.ConcernsFlagged, entity.ReferralMade,
            entity.NotesRestricted != null && entity.NotesRestricted != string.Empty);
    }

    public async Task<ProcessRecordItem?> UpdateAsync(Guid id, CreateProcessRecordRequest request, CancellationToken ct)
    {
        var entity = await db.ProcessRecordings.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return null;

        entity.ResidentCaseId = request.ResidentCaseId;
        entity.RecordedAt = request.RecordedAt ?? entity.RecordedAt;
        entity.SocialWorker = request.SocialWorker;
        entity.SessionType = request.SessionType;
        entity.SessionDurationMinutes = request.SessionDurationMinutes;
        entity.EmotionalStateObserved = request.EmotionalStateObserved;
        entity.EmotionalStateEnd = request.EmotionalStateEnd;
        entity.Summary = request.Summary;
        entity.InterventionsApplied = request.InterventionsApplied;
        entity.FollowUpActions = request.FollowUpActions;
        entity.ProgressNoted = request.ProgressNoted;
        entity.ConcernsFlagged = request.ConcernsFlagged;
        entity.ReferralMade = request.ReferralMade;
        entity.NotesRestricted = request.NotesRestricted;

        await db.SaveChangesAsync(ct);
        return new ProcessRecordItem(
            entity.Id, entity.ResidentCaseId, entity.RecordedAt,
            entity.SocialWorker, entity.SessionType, entity.SessionDurationMinutes,
            entity.EmotionalStateObserved, entity.EmotionalStateEnd, entity.Summary,
            entity.InterventionsApplied, entity.FollowUpActions,
            entity.ProgressNoted, entity.ConcernsFlagged, entity.ReferralMade,
            entity.NotesRestricted != null && entity.NotesRestricted != string.Empty);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.ProcessRecordings.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return false;

        db.ProcessRecordings.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public sealed class VisitationConferenceService(SafeHarborDbContext db) : IVisitationConferenceService
{
    public async Task<PagedResult<HomeVisitItem>> GetVisitsAsync(PagingQuery query, CancellationToken ct)
    {
        var q = db.HomeVisits.AsNoTracking().Include(x => x.VisitType).Include(x => x.StatusState).AsQueryable();

        if (query.ResidentCaseId is { } residentCaseId)
        {
            q = q.Where(x => x.ResidentCaseId == residentCaseId);
        }

        if (query.StatusStateId is { } statusStateId)
        {
            q = q.Where(x => x.StatusStateId == statusStateId);
        }

        q = query.Desc ? q.OrderByDescending(x => x.VisitDate) : q.OrderBy(x => x.VisitDate);

        var total = await q.CountAsync(ct);
        var page = query.NormalizedPage;
        var pageSize = query.NormalizedPageSize;

        var items = await q.Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new HomeVisitItem(
                x.Id,
                x.ResidentCaseId,
                x.VisitDate,
                x.VisitType != null ? x.VisitType.Name : "Unknown",
                x.StatusState != null ? x.StatusState.Name : "Unknown",
                x.Notes))
            .ToArrayAsync(ct);

        return new PagedResult<HomeVisitItem>(items, page, pageSize, total);
    }

    public Task<PagedResult<CaseConferenceItem>> GetUpcomingAsync(PagingQuery query, CancellationToken ct)
        => GetConferencesAsync(query, ct, upcoming: true);

    public Task<PagedResult<CaseConferenceItem>> GetPreviousAsync(PagingQuery query, CancellationToken ct)
        => GetConferencesAsync(query, ct, upcoming: false);

    private async Task<PagedResult<CaseConferenceItem>> GetConferencesAsync(PagingQuery query, CancellationToken ct, bool upcoming)
    {
        var now = DateTimeOffset.UtcNow;
        var q = db.CaseConferences.AsNoTracking().Include(x => x.StatusState).AsQueryable();

        q = upcoming ? q.Where(x => x.ConferenceDate >= now) : q.Where(x => x.ConferenceDate < now);

        if (query.ResidentCaseId is { } residentCaseId)
        {
            q = q.Where(x => x.ResidentCaseId == residentCaseId);
        }

        if (query.StatusStateId is { } statusStateId)
        {
            q = q.Where(x => x.StatusStateId == statusStateId);
        }

        q = upcoming ? q.OrderBy(x => x.ConferenceDate) : q.OrderByDescending(x => x.ConferenceDate);

        var total = await q.CountAsync(ct);
        var page = query.NormalizedPage;
        var pageSize = query.NormalizedPageSize;

        var items = await q.Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CaseConferenceItem(
                x.Id,
                x.ResidentCaseId,
                x.ConferenceDate,
                x.StatusState != null ? x.StatusState.Name : "Unknown",
                x.OutcomeSummary))
            .ToArrayAsync(ct);

        return new PagedResult<CaseConferenceItem>(items, page, pageSize, total);
    }
}

public sealed class DonorContributionService(SafeHarborDbContext db) : IDonorContributionService
{
    public async Task<PagedResult<DonorListItem>> GetDonorsAsync(PagingQuery query, CancellationToken ct)
    {
        var q = db.Donors.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            q = q.Where(x => x.DisplayName.ToLower().Contains(search) || x.Email.ToLower().Contains(search));
        }

        q = query.Desc ? q.OrderByDescending(x => x.LastActivityAt) : q.OrderBy(x => x.LastActivityAt);

        var total = await q.CountAsync(ct);
        var page = query.NormalizedPage;
        var pageSize = query.NormalizedPageSize;
        var items = await q.Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new DonorListItem(x.Id, x.DisplayName, x.Email, x.LastActivityAt, x.LifetimeDonations))
            .ToArrayAsync(ct);

        return new PagedResult<DonorListItem>(items, page, pageSize, total);
    }

    public async Task<DonorListItem> CreateDonorAsync(CreateDonorRequest request, CancellationToken ct)
    {
        var donor = new Donor
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            DisplayName = request.Name,
            Email = request.Email,
            LastActivityAt = DateTimeOffset.UtcNow,
            LifetimeDonations = 0m
        };

        db.Donors.Add(donor);
        await db.SaveChangesAsync(ct);
        return new DonorListItem(donor.Id, donor.DisplayName, donor.Email, donor.LastActivityAt, donor.LifetimeDonations);
    }

    public async Task<ContributionListItem> CreateContributionAsync(CreateContributionRequest request, CancellationToken ct)
    {
        var donor = await db.Donors.FirstOrDefaultAsync(x => x.Id == request.DonorId, ct)
            ?? throw new KeyNotFoundException($"Donor {request.DonorId} was not found.");

        var contribution = new Contribution
        {
            Id = Guid.NewGuid(),
            DonorId = request.DonorId,
            Amount = request.Amount,
            CampaignId = request.CampaignId,
            ContributionDate = request.ContributionDate ?? DateTimeOffset.UtcNow,
            ContributionTypeId = request.ContributionTypeId,
            StatusStateId = request.StatusStateId
        };

        donor.LifetimeDonations += request.Amount;
        donor.LastActivityAt = DateTimeOffset.UtcNow;

        db.Contributions.Add(contribution);
        await db.SaveChangesAsync(ct);

        var statusName = await db.StatusState.AsNoTracking().Where(x => x.Id == request.StatusStateId).Select(x => x.Name).FirstOrDefaultAsync(ct) ?? "Unknown";
        return new ContributionListItem(contribution.Id, donor.DisplayName, contribution.Amount, contribution.ContributionDate, statusName);
    }

    public async Task<bool> CreateAllocationAsync(CreateAllocationRequest request, CancellationToken ct)
    {
        var contributionExists = await db.Contributions.AnyAsync(x => x.Id == request.ContributionId, ct);
        var safehouseExists = await db.Safehouses.AnyAsync(x => x.Id == request.SafehouseId, ct);
        if (!contributionExists || !safehouseExists)
        {
            return false;
        }

        // Keep allocations explicit in the database so donation analytics can compare funding distribution by safehouse.
        db.Set<ContributionAllocation>().Add(new ContributionAllocation
        {
            Id = Guid.NewGuid(),
            ContributionId = request.ContributionId,
            SafehouseId = request.SafehouseId,
            AmountAllocated = request.AmountAllocated
        });

        await db.SaveChangesAsync(ct);
        return true;
    }
}

public sealed class ReportsAnalyticsService(SafeHarborDbContext db) : IReportsAnalyticsService
{
    public async Task<ReportsAnalyticsResponse> GetAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var sixMonthsAgo = now.AddMonths(-5);

        var donations = await db.Contributions.AsNoTracking()
            .Where(x => x.ContributionDate >= sixMonthsAgo)
            .ToArrayAsync(ct);

        var visits = await db.HomeVisits.AsNoTracking()
            .Where(x => x.VisitDate >= sixMonthsAgo)
            .ToArrayAsync(ct);

        var residentCases = await db.ResidentCases.AsNoTracking().Include(x => x.Safehouse).ToArrayAsync(ct);
        var allocations = await db.Set<ContributionAllocation>().AsNoTracking().ToArrayAsync(ct);

        var donationTrends = donations
            .GroupBy(x => x.ContributionDate.ToString("yyyy-MM"))
            .OrderBy(x => x.Key)
            .Select(x => new DonationTrendPoint(x.Key, x.Sum(v => v.Amount)))
            .ToArray();

        var outcomeTrends = visits
            .GroupBy(x => x.VisitDate.ToString("yyyy-MM"))
            .OrderBy(x => x.Key)
            .Select(x => new OutcomeTrendPoint(x.Key, residentCases.Count(rc => rc.OpenedAt.ToString("yyyy-MM") == x.Key), x.Count()))
            .ToArray();

        var safehouseComparisons = residentCases
            .GroupBy(x => x.SafehouseId)
            .Select(g =>
            {
                var safehouseName = g.First().Safehouse?.Name ?? "Unknown";
                var allocationTotal = allocations.Where(a => a.SafehouseId == g.Key).Sum(a => a.AmountAllocated);
                return new SafehouseComparisonItem(safehouseName, g.Count(x => x.ClosedAt == null), allocationTotal);
            })
            .OrderByDescending(x => x.AllocatedFunding)
            .ToArray();

        var posts = await db.SocialPostMetrics.AsNoTracking().ToArrayAsync(ct);

        var platform = BuildCorrelation(posts, x => x.Platform);
        var contentType = BuildCorrelation(posts, x => x.ContentType);
        var postingHour = BuildCorrelation(posts, x => x.PostedAt.Hour.ToString("00") + ":00");

        var topPosts = posts
            .OrderByDescending(x => x.AttributedDonationAmount ?? 0)
            .Take(5)
            .Select(x => new SocialPostDonationInsight(
                x.Id,
                x.PostedAt,
                x.Platform,
                x.ContentType,
                x.Reach,
                x.Engagements,
                x.AttributedDonationAmount,
                x.AttributedDonationCount,
                x.Reach > 0 ? Math.Round((decimal)x.Engagements / x.Reach * 100m, 2) : 0m))
            .ToArray();

        var recommendations = new List<ContentTimingRecommendationCard>();
        var bestPlatform = platform.OrderByDescending(x => x.TotalAttributedDonationAmount).FirstOrDefault();
        if (bestPlatform is not null)
        {
            recommendations.Add(new ContentTimingRecommendationCard(
                "Prioritize strongest platform",
                $"{bestPlatform.Group} currently drives the highest attributed donation volume.",
                "Increase post volume on that platform while validating attribution over the next month."));
        }

        return new ReportsAnalyticsResponse(
            donationTrends,
            outcomeTrends,
            safehouseComparisons,
            Array.Empty<ReintegrationRatePoint>(),
            platform,
            contentType,
            postingHour,
            topPosts,
            recommendations);
    }

    private static SocialDonationCorrelationPoint[] BuildCorrelation(IEnumerable<SocialPostMetric> metrics, Func<SocialPostMetric, string> groupBy)
    {
        return metrics
            .GroupBy(groupBy)
            .Select(g =>
            {
                var totalReach = g.Sum(x => x.Reach);
                var totalAttributed = g.Sum(x => x.AttributedDonationAmount ?? 0);
                var totalEngagements = g.Sum(x => x.Engagements);
                var donationCount = g.Sum(x => x.AttributedDonationCount ?? 0);
                var per1k = totalReach > 0 ? totalAttributed / (totalReach / 1000m) : 0m;
                var rate = totalReach > 0 ? (decimal)totalEngagements / totalReach * 100m : 0m;
                return new SocialDonationCorrelationPoint(g.Key, g.Count(), totalReach, totalEngagements, totalAttributed, donationCount, Math.Round(per1k, 2), Math.Round(rate, 2));
            })
            .OrderByDescending(x => x.TotalAttributedDonationAmount)
            .ToArray();
    }
}
