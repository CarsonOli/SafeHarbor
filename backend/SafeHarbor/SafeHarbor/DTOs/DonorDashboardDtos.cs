namespace SafeHarbor.DTOs;

// ── Donor Dashboard Response DTOs ────────────────────────────────────────────
// These records define the JSON shape returned by GET /api/donor/dashboard.
// The frontend TypeScript types in src/types/impact.ts mirror this structure.
//
// All records are immutable (sealed record) to prevent accidental mutation
// during serialization and to make the contracts explicit for future ML integration.

/// <summary>
/// Top-level response for the donor dashboard.
/// Returned by GET /api/donor/dashboard for the authenticated donor.
/// </summary>
/// <param name="DonorName">Display name shown in the hero section greeting.</param>
/// <param name="LifetimeDonated">Sum of all completed contributions by this donor, in USD.</param>
/// <param name="MonthlyHistory">
///   12-entry array covering the past 12 calendar months in ascending order.
///   Months with no donation have Amount = 0 (zero-filled on the server so the
///   frontend bar chart always has a consistent 12-bar axis).
/// </param>
/// <param name="ActiveCampaign">
///   The current active campaign, or null if no campaign is running.
///   Used to render the goal progress bar.
/// </param>
/// <param name="Impact">
///   Impact score derived from the donor's lifetime total.
///   Calculated by the injected <c>IDonorImpactCalculator</c> — can be ML-powered
///   by swapping the DI registration in Program.cs.
/// </param>
public sealed record DonorDashboardResponse(
    string DonorName,
    decimal LifetimeDonated,
    IReadOnlyList<MonthlyDonationPoint> MonthlyHistory,
    CampaignGoalSummary? ActiveCampaign,
    DonorImpactSummary Impact);

/// <summary>
/// A single month's donation total for the bar chart.
/// </summary>
/// <param name="Month">ISO year-month string, e.g. "2025-11". Used as the chart axis label.</param>
/// <param name="Amount">Total donated in this month, in USD. 0 for months with no donation.</param>
public sealed record MonthlyDonationPoint(string Month, decimal Amount);

/// <summary>
/// Campaign fundraising goal summary, shown as a progress bar on the donor dashboard.
/// </summary>
/// <param name="CampaignId">Unique identifier of the campaign.</param>
/// <param name="CampaignName">Human-readable campaign title shown as the section heading.</param>
/// <param name="GoalAmount">Fundraising target for the campaign, in USD.</param>
/// <param name="TotalRaisedAllDonors">
///   Sum of all completed contributions linked to this campaign (across all donors).
///   Used to fill the progress bar and show the campaign-wide total.
/// </param>
/// <param name="ThisDonorContributed">Amount this specific donor has given to the campaign.</param>
/// <param name="ProgressPercent">
///   TotalRaisedAllDonors / GoalAmount * 100, capped at 100.
///   Directly used as the CSS --bar-width custom property value on the frontend.
/// </param>
public sealed record CampaignGoalSummary(
    Guid CampaignId,
    string CampaignName,
    decimal GoalAmount,
    decimal TotalRaisedAllDonors,
    decimal ThisDonorContributed,
    decimal ProgressPercent);

/// <summary>
/// The donor's estimated real-world impact.
/// Produced by <c>IDonorImpactCalculator</c>; the ModelVersion field tells the
/// frontend (and donors) which calculation method was used.
/// </summary>
/// <param name="GirlsHelped">Estimated number of girls supported through this donor's giving.</param>
/// <param name="ImpactLabel">Descriptive label shown below the number, e.g. "girls supported toward safe housing".</param>
/// <param name="ModelVersion">Identifier of the calculation model, e.g. "rule-based-v1" or "ml-v2".</param>
public sealed record DonorImpactSummary(
    int GirlsHelped,
    string ImpactLabel,
    string ModelVersion);

// ── New Contribution Request ──────────────────────────────────────────────────

/// <summary>
/// Request body for POST /api/donor/contribution.
/// Allows a logged-in donor to record an additional donation.
/// </summary>
/// <param name="Email">
///   Deprecated and ignored by the API.
///   Donor identity is resolved from authenticated claims to prevent horizontal access.
///   This field remains optional for backward compatibility with older clients.
/// </param>
/// <param name="Amount">Donation amount in USD. Must be greater than zero.</param>
/// <param name="CampaignId">
///   Optional campaign to associate this donation with.
///   If omitted, the controller auto-assigns to the currently active campaign.
/// </param>
public sealed record NewContributionRequest(
    decimal Amount,
    Guid? CampaignId = null,
    string? Email = null);

/// <summary>
/// Response body for a successfully recorded contribution.
/// </summary>
/// <param name="ContributionId">The newly created contribution's GUID.</param>
/// <param name="Message">A human-readable confirmation message displayed on the donor dashboard.</param>
public sealed record NewContributionResponse(Guid ContributionId, string Message);
