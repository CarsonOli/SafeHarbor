using System.ComponentModel.DataAnnotations;

namespace SafeHarbor.DTOs;

public sealed record DashboardSummaryResponse(
    int ActiveResidents,
    IReadOnlyCollection<ContributionListItem> RecentContributions,
    IReadOnlyCollection<ConferenceListItem> UpcomingConferences,
    IReadOnlyCollection<OutcomeSummaryItem> SummaryOutcomes);

public sealed record ContributionListItem(Guid Id, string DonorName, decimal Amount, DateTimeOffset ContributionDate, string Status);
public sealed record ConferenceListItem(Guid Id, Guid ResidentCaseId, DateTimeOffset ConferenceDate, string Status, string OutcomeSummary);
public sealed record OutcomeSummaryItem(DateOnly SnapshotDate, int TotalResidentsServed, int TotalHomeVisits, decimal TotalContributions);

public sealed record DonorListItem(Guid Id, string Name, string Email, DateTimeOffset LastActivityAt, decimal LifetimeContributions);
public sealed record CreateDonorRequest([property: Required, StringLength(120, MinimumLength = 2)] string Name, [property: Required, EmailAddress] string Email);

public sealed record CreateContributionRequest(
    [property: Required] Guid DonorId,
    [property: Range(typeof(decimal), "0.01", "1000000000")] decimal Amount,
    [property: Required] int ContributionTypeId,
    [property: Required] int StatusStateId,
    DateTimeOffset? ContributionDate,
    Guid? CampaignId);

public sealed record CreateAllocationRequest(
    [property: Required] Guid ContributionId,
    [property: Required] Guid SafehouseId,
    [property: Range(typeof(decimal), "0.01", "1000000000")] decimal AmountAllocated);

public sealed record ResidentCaseListItem(
    Guid Id,
    Guid SafehouseId,
    string Safehouse,
    int CaseCategoryId,
    string Category,
    int StatusStateId,
    string Status,
    string? SocialWorkerExternalId,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt);

public sealed record CreateResidentCaseRequest(
    [property: Required] Guid SafehouseId,
    [property: Required] int CaseCategoryId,
    int? CaseSubcategoryId,
    [property: Required] int StatusStateId,
    Guid? ResidentUserId,
    DateTimeOffset? OpenedAt);

public sealed record UpdateResidentCaseRequest(
    [property: Required] Guid SafehouseId,
    [property: Required] int CaseCategoryId,
    int? CaseSubcategoryId,
    [property: Required] int StatusStateId,
    Guid? ResidentUserId,
    DateTimeOffset? ClosedAt);

public sealed record ProcessRecordItem(Guid Id, Guid ResidentCaseId, DateTimeOffset RecordedAt, string Summary);
public sealed record CreateProcessRecordRequest([property: Required] Guid ResidentCaseId, [property: StringLength(4000, MinimumLength = 3)] string Summary, DateTimeOffset? RecordedAt);

public sealed record HomeVisitItem(Guid Id, Guid ResidentCaseId, DateTimeOffset VisitDate, string VisitType, string Status, string Notes);
public sealed record CaseConferenceItem(Guid Id, Guid ResidentCaseId, DateTimeOffset ConferenceDate, string Status, string OutcomeSummary);

public sealed record DonationTrendPoint(string Month, decimal Amount);
public sealed record OutcomeTrendPoint(string Month, int ResidentsServed, int HomeVisits);
public sealed record SafehouseComparisonItem(string Safehouse, int ActiveResidents, decimal AllocatedFunding);
public sealed record ReintegrationRatePoint(string Month, decimal RatePercent);

public sealed record ReportsAnalyticsResponse(
    IReadOnlyCollection<DonationTrendPoint> DonationTrends,
    IReadOnlyCollection<OutcomeTrendPoint> OutcomeTrends,
    IReadOnlyCollection<SafehouseComparisonItem> SafehouseComparisons,
    IReadOnlyCollection<ReintegrationRatePoint> ReintegrationRates);
