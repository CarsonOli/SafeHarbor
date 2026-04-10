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
public sealed record CreateDonorRequest([param: Required, StringLength(120, MinimumLength = 2)] string Name, [param: Required, EmailAddress] string Email);

public sealed record CreateContributionRequest(
    [param: Required] Guid DonorId,
    [param: Range(typeof(decimal), "0.01", "1000000000")] decimal Amount,
    [param: Required] int ContributionTypeId,
    [param: Required] int StatusStateId,
    DateTimeOffset? ContributionDate,
    Guid? CampaignId);

public sealed record CreateAllocationRequest(
    [param: Required] Guid ContributionId,
    [param: Required] Guid SafehouseId,
    [param: Range(typeof(decimal), "0.01", "1000000000")] decimal AmountAllocated);

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
    [param: Required] Guid SafehouseId,
    [param: Required] int CaseCategoryId,
    int? CaseSubcategoryId,
    [param: Required] int StatusStateId,
    Guid? ResidentUserId,
    DateTimeOffset? OpenedAt);

public sealed record UpdateResidentCaseRequest(
    [param: Required] Guid SafehouseId,
    [param: Required] int CaseCategoryId,
    int? CaseSubcategoryId,
    [param: Required] int StatusStateId,
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
    [param: Required] Guid ResidentCaseId,
    [param: Required, StringLength(120, MinimumLength = 2)] string SocialWorker,
    [param: Required] string SessionType,
    int? SessionDurationMinutes,
    [param: Required, StringLength(60, MinimumLength = 2)] string EmotionalStateObserved,
    string? EmotionalStateEnd,
    [param: Required, StringLength(8000, MinimumLength = 3)] string Summary,
    string? InterventionsApplied,
    string? FollowUpActions,
    bool ProgressNoted,
    bool ConcernsFlagged,
    bool ReferralMade,
    string? NotesRestricted,
    DateTimeOffset? RecordedAt);

public sealed record HomeVisitItem(
    Guid Id,
    Guid ResidentCaseId,
    DateTimeOffset VisitDate,
    string VisitType,
    string Status,
    string HomeEnvironmentObservations,
    string FamilyCooperationLevel,
    bool SafetyConcernsIdentified,
    string FollowUpActions,
    string Notes);
public sealed record CaseConferenceItem(Guid Id, Guid ResidentCaseId, DateTimeOffset ConferenceDate, string Status, string OutcomeSummary);

public sealed record CreateHomeVisitRequest(
    [param: Required] Guid ResidentCaseId,
    [param: Required] int VisitTypeId,
    [param: Required] int StatusStateId,
    [param: Required] DateTimeOffset VisitDate,
    string? HomeEnvironmentObservations,
    string? FamilyCooperationLevel,
    bool SafetyConcernsIdentified,
    string? FollowUpActions,
    string? Notes);

public sealed record CreateCaseConferenceRequest(
    [param: Required] Guid ResidentCaseId,
    [param: Required] int StatusStateId,
    [param: Required] DateTimeOffset ConferenceDate,
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
    [param: Required] DateTimeOffset PostedAt,
    [param: Required, StringLength(80, MinimumLength = 2)] string Platform,
    [param: Required, StringLength(80, MinimumLength = 2)] string ContentType,
    [param: Range(0, int.MaxValue)] int Reach,
    [param: Range(0, int.MaxValue)] int Engagements,
    [param: Range(typeof(decimal), "0", "1000000000")] decimal? AttributedDonationAmount,
    [param: Range(0, int.MaxValue)] int? AttributedDonationCount);

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

