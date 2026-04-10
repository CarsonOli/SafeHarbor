using System.ComponentModel.DataAnnotations;

namespace SafeHarbor.DTOs;

public sealed record DashboardSummaryResponse(
    int ActiveResidents,
    IReadOnlyCollection<ContributionListItem> RecentContributions,
    IReadOnlyCollection<ConferenceListItem> UpcomingConferences,
    IReadOnlyCollection<OutcomeSummaryItem> SummaryOutcomes);

public sealed record NotImplementedEnvelope(
    string ErrorCode,
    string Message,
    string TraceId,
    string ApiVersion);

public sealed record ContributionListItem(Guid Id, string DonorName, decimal Amount, DateTimeOffset ContributionDate, string Status);
public sealed record ConferenceListItem(Guid Id, Guid ResidentCaseId, DateTimeOffset ConferenceDate, string Status, string OutcomeSummary);
public sealed record OutcomeSummaryItem(DateOnly SnapshotDate, int TotalResidentsServed, int TotalHomeVisits, decimal TotalContributions);

public sealed record CaseloadLookupItem(int Id, string Name);
public sealed record CaseloadSafehouseItem(string Id, string Name);
public sealed record CaseloadLookupsResponse(
    IReadOnlyCollection<CaseloadSafehouseItem> Safehouses,
    IReadOnlyCollection<CaseloadLookupItem> CaseCategories,
    IReadOnlyCollection<CaseloadLookupItem> StatusStates);

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
    string? ResidentName,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    Guid? ResidentEntityId = null);

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

public sealed record ProcessRecordItem(
    Guid Id,
    Guid ResidentCaseId,
    DateTimeOffset RecordedAt,
    string SocialWorker,
    string SessionType,
    int? SessionDurationMinutes,
    string EmotionalStateObserved,
    string? EmotionalStateEnd,
    string Summary,
    string? InterventionsApplied,
    string? FollowUpActions,
    bool ProgressNoted,
    bool ConcernsFlagged,
    bool ReferralMade,
    bool NotesRestricted);

public sealed record CreateProcessRecordRequest(
    [property: Required] Guid ResidentCaseId,
    [property: Required, StringLength(120, MinimumLength = 2)] string SocialWorker,
    [property: Required] string SessionType,
    int? SessionDurationMinutes,
    [property: Required, StringLength(60, MinimumLength = 2)] string EmotionalStateObserved,
    string? EmotionalStateEnd,
    [property: Required, StringLength(8000, MinimumLength = 3)] string Summary,
    string? InterventionsApplied,
    string? FollowUpActions,
    bool ProgressNoted,
    bool ConcernsFlagged,
    bool ReferralMade,
    string? NotesRestricted,
    DateTimeOffset? RecordedAt);

public sealed record HomeVisitItem(Guid Id, Guid ResidentCaseId, DateTimeOffset VisitDate, string VisitType, string Status, string Notes);
public sealed record CaseConferenceItem(Guid Id, Guid ResidentCaseId, DateTimeOffset ConferenceDate, string Status, string OutcomeSummary);

public sealed record CreateHomeVisitRequest(
    [property: Required] Guid ResidentCaseId,
    [property: Required] int VisitTypeId,
    [property: Required] int StatusStateId,
    [property: Required] DateTimeOffset VisitDate,
    string Notes);

public sealed record CreateCaseConferenceRequest(
    [property: Required] Guid ResidentCaseId,
    [property: Required] int StatusStateId,
    [property: Required] DateTimeOffset ConferenceDate,
    string OutcomeSummary);

public sealed record DonationTrendPoint(string Month, decimal Amount);
public sealed record OutcomeTrendPoint(string Month, int ResidentsServed, int HomeVisits);
public sealed record SafehouseComparisonItem(string Safehouse, int ActiveResidents, decimal AllocatedFunding);
public sealed record ReintegrationRatePoint(string Month, decimal RatePercent);

public sealed record ReportsAnalyticsResponse(
    IReadOnlyCollection<DonationTrendPoint> DonationTrends,
    IReadOnlyCollection<OutcomeTrendPoint> OutcomeTrends,
    IReadOnlyCollection<SafehouseComparisonItem> SafehouseComparisons,
    IReadOnlyCollection<ReintegrationRatePoint> ReintegrationRates,
    IReadOnlyCollection<SocialDonationCorrelationPoint> DonationCorrelationByPlatform,
    IReadOnlyCollection<SocialDonationCorrelationPoint> DonationCorrelationByContentType,
    IReadOnlyCollection<SocialDonationCorrelationPoint> DonationCorrelationByPostingHour,
    IReadOnlyCollection<SocialPostDonationInsight> TopAttributedPosts,
    IReadOnlyCollection<ContentTimingRecommendationCard> Recommendations);


public sealed record SocialPostMetricListItem(
    Guid Id,
    Guid? CampaignId,
    DateTimeOffset PostedAt,
    string Platform,
    string ContentType,
    int Reach,
    int Engagements,
    decimal? AttributedDonationAmount,
    int? AttributedDonationCount);

public sealed record CreateSocialPostMetricRequest(
    Guid? CampaignId,
    [property: Required] DateTimeOffset PostedAt,
    [property: Required, StringLength(80, MinimumLength = 2)] string Platform,
    [property: Required, StringLength(80, MinimumLength = 2)] string ContentType,
    [property: Range(0, int.MaxValue)] int Reach,
    [property: Range(0, int.MaxValue)] int Engagements,
    [property: Range(typeof(decimal), "0", "1000000000")] decimal? AttributedDonationAmount,
    [property: Range(0, int.MaxValue)] int? AttributedDonationCount);

public sealed record SocialDonationCorrelationPoint(
    string Group,
    int Posts,
    int TotalReach,
    int TotalEngagements,
    decimal TotalAttributedDonationAmount,
    int TotalAttributedDonationCount,
    decimal DonationsPer1kReach,
    decimal EngagementRatePercent);

public sealed record SocialPostDonationInsight(
    Guid PostMetricId,
    DateTimeOffset PostedAt,
    string Platform,
    string ContentType,
    int Reach,
    int Engagements,
    decimal? AttributedDonationAmount,
    int? AttributedDonationCount,
    decimal EngagementRatePercent);

public sealed record ContentTimingRecommendationCard(
    string Title,
    string Rationale,
    string Action);

