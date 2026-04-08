using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using SafeHarbor.DTOs;
using SafeHarbor.Infrastructure;

namespace SafeHarbor.Controllers.Admin;

/// <summary>
/// Provides aggregated donor analytics for the admin dashboard.
///
/// ENDPOINT:
///   GET /api/admin/donor-analytics
///
/// Returns KPIs (total donations, donor count, retention rate, average gift),
/// a 12-month monthly trend for the line chart, per-campaign OKR metrics,
/// and a top-donors leaderboard.
///
/// AUTH NOTE:
///   This endpoint is restricted with PolicyNames.AdminOnly because it surfaces
///   cross-donor financial aggregates intended for admin reporting workflows.
/// </summary>
[ApiController]
[Route("api/admin/donor-analytics")]
[Authorize(Policy = PolicyNames.AdminOnly)]
public sealed class AdminDonorAnalyticsController(InMemoryDataStore store) : ControllerBase
{
    // StatusStateId = 1 means "Completed" for contributions.
    // Must match the value used in DonorDashboardSeeder and DonorDashboardController.
    // TODO: Replace with a named lookup once the database is wired.
    private const int CompletedContributionStatusId = 1;

    // A donor is considered "active" if they gave within the last 90 days.
    private const int ActiveWindowDays = 90;

    // Limit leaderboard to the top N donors.
    private const int TopDonorLimit = 5;

    /// <summary>
    /// Returns all aggregated donor analytics for the admin dashboard.
    /// Aggregations are computed from the InMemoryDataStore at request time.
    /// </summary>
    [HttpGet]
    public ActionResult<DonorAnalyticsResponse> GetAnalytics()
    {
        var now = DateTimeOffset.UtcNow;

        // Only count completed contributions throughout all aggregations.
        var completedContributions = store.Contributions
            .Where(c => c.StatusStateId == CompletedContributionStatusId)
            .ToList();

        // ── KPI: Total donations ───────────────────────────────────────────
        var totalDonationsReceived = completedContributions.Sum(c => c.Amount);
        var totalContributionCount = completedContributions.Count;

        // ── KPI: Donor counts ──────────────────────────────────────────────
        var totalDonorCount = store.Donors.Count;

        // Active = gave at least once in the last 90 days.
        var activeCutoff = now.AddDays(-ActiveWindowDays);
        var activeDonorIds = completedContributions
            .Where(c => c.ContributionDate >= activeCutoff)
            .Select(c => c.DonorId)
            .Distinct()
            .ToHashSet();
        var activeDonorCount = activeDonorIds.Count;

        // ── KPI: Retention rate ────────────────────────────────────────────
        // Retention = % of donors who have given more than once.
        // A donor who only donated once has not been "retained" for a second gift.
        var retentionRate = 0m;
        if (totalDonorCount > 0)
        {
            var repeatDonorCount = completedContributions
                .GroupBy(c => c.DonorId)
                .Count(g => g.Count() >= 2);
            retentionRate = Math.Round((decimal)repeatDonorCount / totalDonorCount * 100, 1);
        }

        // ── KPI: Average gift size ─────────────────────────────────────────
        var averageGiftSize = totalContributionCount > 0
            ? Math.Round(totalDonationsReceived / totalContributionCount, 2)
            : 0m;

        // ── Monthly trend (12 months, zero-filled) ─────────────────────────
        // Used by the SVG line chart on the frontend.
        // Each entry includes: total amount raised + new donors that month.
        var monthlyTrend = BuildMonthlyTrend(completedContributions, now);

        // ── Campaign OKRs ──────────────────────────────────────────────────
        var campaigns = BuildCampaignSummaries(completedContributions);

        // ── Top donors leaderboard ─────────────────────────────────────────
        var topDonors = BuildTopDonors(completedContributions);

        return Ok(new DonorAnalyticsResponse(
            TotalDonationsReceived: totalDonationsReceived,
            TotalDonorCount: totalDonorCount,
            ActiveDonorCount: activeDonorCount,
            RetentionRate: retentionRate,
            AverageGiftSize: averageGiftSize,
            TotalContributionCount: totalContributionCount,
            MonthlyTrend: monthlyTrend,
            Campaigns: campaigns,
            TopDonors: topDonors));
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a zero-filled 12-month trend array for the line chart.
    /// Each entry includes the total raised and the number of first-time donors in that month.
    /// </summary>
    private static IReadOnlyList<AnalyticsMonthlyPoint> BuildMonthlyTrend(
        IReadOnlyList<Models.Entities.Contribution> contributions,
        DateTimeOffset reference)
    {
        // Group amounts by "yyyy-MM" for fast lookup.
        var amountByMonth = contributions
            .GroupBy(c => c.ContributionDate.ToString("yyyy-MM"))
            .ToDictionary(g => g.Key, g => g.Sum(c => c.Amount));

        // To find new donors per month: for each donor, find their first contribution month.
        var firstMonthPerDonor = contributions
            .GroupBy(c => c.DonorId)
            .ToDictionary(
                g => g.Key,
                g => g.Min(c => c.ContributionDate).ToString("yyyy-MM"));

        // Count first-time donors per month.
        var newDonorsByMonth = firstMonthPerDonor.Values
            .GroupBy(m => m)
            .ToDictionary(g => g.Key, g => g.Count());

        var result = new List<AnalyticsMonthlyPoint>(12);

        // Walk backwards 11 months, then forward — produces ascending order.
        for (int i = 11; i >= 0; i--)
        {
            var month = reference.AddMonths(-i);
            var key = month.ToString("yyyy-MM");

            result.Add(new AnalyticsMonthlyPoint(
                Month: key,
                Amount: amountByMonth.TryGetValue(key, out var amount) ? amount : 0m,
                NewDonors: newDonorsByMonth.TryGetValue(key, out var newDonors) ? newDonors : 0));
        }

        return result;
    }

    /// <summary>
    /// Builds a campaign OKR summary for each campaign in the store.
    /// Shows goal, amount raised, progress percentage, and donor/contribution counts.
    /// </summary>
    private IReadOnlyList<CampaignAnalyticsSummary> BuildCampaignSummaries(
        IReadOnlyList<Models.Entities.Contribution> contributions)
    {
        return store.Campaigns
            .Select(campaign =>
            {
                // All completed contributions linked to this campaign.
                var campaignContributions = contributions
                    .Where(c => c.CampaignId == campaign.Id)
                    .ToList();

                var totalRaised = campaignContributions.Sum(c => c.Amount);
                var donorCount = campaignContributions.Select(c => c.DonorId).Distinct().Count();

                // Progress capped at 100% so an over-funded campaign doesn't break the progress bar.
                var progressPercent = campaign.GoalAmount > 0
                    ? Math.Min(100m, Math.Round(totalRaised / campaign.GoalAmount * 100, 1))
                    : 0m;

                return new CampaignAnalyticsSummary(
                    CampaignId: campaign.Id,
                    CampaignName: campaign.Name,
                    GoalAmount: campaign.GoalAmount,
                    TotalRaised: totalRaised,
                    ProgressPercent: progressPercent,
                    DonorCount: donorCount,
                    ContributionCount: campaignContributions.Count);
            })
            .OrderByDescending(c => c.TotalRaised) // most-funded campaigns first
            .ToList();
    }

    /// <summary>
    /// Returns the top N donors ranked by lifetime donations.
    /// Joins contribution totals to donor display names.
    /// </summary>
    private IReadOnlyList<TopDonorSummary> BuildTopDonors(
        IReadOnlyList<Models.Entities.Contribution> contributions)
    {
        // Group contributions by donor, sum amounts, join to donor name.
        var donorTotals = contributions
            .GroupBy(c => c.DonorId)
            .Select(g =>
            {
                var donor = store.Donors.FirstOrDefault(d => d.Id == g.Key);
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

        return donorTotals;
    }
}
