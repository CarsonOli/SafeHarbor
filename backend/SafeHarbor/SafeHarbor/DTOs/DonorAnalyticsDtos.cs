namespace SafeHarbor.DTOs;

// ── Admin Donor Analytics Response DTOs ──────────────────────────────────────
// Returned by GET /api/admin/donor-analytics.
// The frontend TypeScript types in src/types/impact.ts mirror this structure.
//
// DESIGN NOTE: All aggregations are computed on the server via domain services/repositories
// so the frontend stays purely presentational regardless of the active persistence provider.

/// <summary>
/// Full response for the admin donor analytics dashboard.
/// Provides KPIs, monthly trend data for the line chart, campaign OKRs, and top donors.
/// </summary>
/// <param name="TotalDonationsReceived">Sum of all completed contributions across all donors, in USD.</param>
/// <param name="TotalDonorCount">Total number of unique donors in the system.</param>
/// <param name="ActiveDonorCount">Donors with at least one contribution in the last 90 days.</param>
/// <param name="RetentionRate">
///   Percentage of donors who have given more than once.
///   Formula: (donors with ≥2 contributions / total donors) × 100.
/// </param>
/// <param name="AverageGiftSize">Mean contribution amount across all completed contributions, in USD.</param>
/// <param name="TotalContributionCount">Total number of completed contribution records.</param>
/// <param name="MonthlyTrend">
///   12-entry array of monthly totals for the line chart, ascending order.
///   Zero-filled for months with no contributions so the chart always has 12 data points.
/// </param>
/// <param name="Campaigns">Per-campaign OKR summary for the campaign progress section.</param>
/// <param name="TopDonors">Top 5 donors ranked by lifetime total (for the leaderboard list).</param>
public sealed record DonorAnalyticsResponse(
    decimal TotalDonationsReceived,
    int TotalDonorCount,
    int ActiveDonorCount,
    decimal RetentionRate,
    decimal AverageGiftSize,
    int TotalContributionCount,
    IReadOnlyList<AnalyticsMonthlyPoint> MonthlyTrend,
    IReadOnlyList<CampaignAnalyticsSummary> Campaigns,
    IReadOnlyList<TopDonorSummary> TopDonors);

/// <summary>
/// A single month's aggregated donation data for the line chart.
/// </summary>
/// <param name="Month">ISO year-month string, e.g. "2025-11".</param>
/// <param name="Amount">Total donations received in this month, in USD. 0 for months with no activity.</param>
/// <param name="NewDonors">Number of donors who made their first-ever contribution in this month.</param>
public sealed record AnalyticsMonthlyPoint(string Month, decimal Amount, int NewDonors);

/// <summary>
/// Per-campaign OKR metrics shown in the Campaign OKRs section.
/// </summary>
/// <param name="CampaignId">Unique identifier of the campaign.</param>
/// <param name="CampaignName">Human-readable campaign name (section heading).</param>
/// <param name="GoalAmount">Fundraising target, in USD.</param>
/// <param name="TotalRaised">Sum of all completed contributions linked to this campaign.</param>
/// <param name="ProgressPercent">TotalRaised / GoalAmount × 100, capped at 100. Used as CSS --stack-width.</param>
/// <param name="DonorCount">Number of unique donors who contributed to this campaign.</param>
/// <param name="ContributionCount">Total number of contribution records linked to this campaign.</param>
public sealed record CampaignAnalyticsSummary(
    Guid CampaignId,
    string CampaignName,
    decimal GoalAmount,
    decimal TotalRaised,
    decimal ProgressPercent,
    int DonorCount,
    int ContributionCount);

/// <summary>
/// A single top-donor entry for the leaderboard list.
/// </summary>
/// <param name="DisplayName">Donor's display name.</param>
/// <param name="LifetimeDonated">Total USD donated across all completed contributions.</param>
/// <param name="ContributionCount">Number of completed contributions made by this donor.</param>
public sealed record TopDonorSummary(
    string DisplayName,
    decimal LifetimeDonated,
    int ContributionCount);
