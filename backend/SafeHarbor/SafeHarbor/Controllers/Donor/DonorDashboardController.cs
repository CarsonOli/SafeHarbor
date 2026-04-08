using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using SafeHarbor.DTOs;
using SafeHarbor.Infrastructure;
using SafeHarbor.Models.Entities;
using SafeHarbor.Services.DonorImpact;

namespace SafeHarbor.Controllers.Donor;

/// <summary>
/// Handles donor-facing dashboard data and donation submissions.
///
/// ENDPOINTS:
///   GET  /api/donor/dashboard?email={email}  — Returns full dashboard data for one donor.
///   POST /api/donor/contribution              — Records a new donation for a donor.
///
/// AUTHENTICATION NOTE:
///   This endpoint now requires an authenticated principal via PolicyNames.AuthenticatedUser
///   so donor routes no longer run anonymously.
///   TODO: Tighten to PolicyNames.DonorOnly once Microsoft Entra ID role claims are wired.
///   TODO: Read email from the JWT claim instead of the query/body param:
///           var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
/// </summary>
[ApiController]
[Route("api/donor")]
[Authorize(Policy = PolicyNames.AuthenticatedUser)]
public sealed class DonorDashboardController(
    InMemoryDataStore store,
    IDonorImpactCalculator impactCalculator) : ControllerBase
{
    // StatusStateId value for "Completed" contributions.
    // These integers match the seeded lookup data in DonorDashboardSeeder.
    // TODO: Replace with named lookups from the database once infrastructure is available.
    private const int CompletedContributionStatusId = 1;
    private const int ActiveCampaignStatusId = 1;
    private const int OnlineDonationTypeId = 1;

    /// <summary>
    /// Returns the full donor dashboard for the given email address.
    ///
    /// The frontend passes the email from the localStorage session.
    /// The response includes lifetime totals, 12-month history, campaign goal progress,
    /// and an impact score (girls helped) calculated by the injected IDonorImpactCalculator.
    /// </summary>
    /// <param name="email">The donor's email address (from the frontend session).</param>
    [HttpGet("dashboard")]
    public ActionResult<DonorDashboardResponse> GetDashboard([FromQuery] string? email)
    {
        // Validate the required email param.
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { error = "email query parameter is required." });

        // Look up the donor by email. Case-insensitive to tolerate minor login differences.
        var donor = store.Donors.FirstOrDefault(
            d => string.Equals(d.Email, email, StringComparison.OrdinalIgnoreCase));

        if (donor is null)
            return NotFound(new { error = $"No donor found with email '{email}'." });

        // Get all completed contributions for this donor.
        var donorContributions = store.Contributions
            .Where(c => c.DonorId == donor.Id && c.StatusStateId == CompletedContributionStatusId)
            .ToList();

        // Lifetime total: sum of all completed contributions.
        var lifetimeDonated = donorContributions.Sum(c => c.Amount);

        // Build the 12-month history (zero-filled so the chart always has 12 bars).
        var monthlyHistory = BuildMonthlyHistory(donorContributions);

        // Find the active campaign and build the goal summary.
        var campaignSummary = BuildCampaignGoalSummary(donor.Id);

        // Calculate the impact score via the injected service.
        // To swap in an ML model, update the DI registration in Program.cs.
        var impact = impactCalculator.Calculate(lifetimeDonated);
        var impactSummary = new DonorImpactSummary(
            impact.GirlsHelped,
            impact.ImpactLabel,
            impact.ModelVersion);

        return Ok(new DonorDashboardResponse(
            DonorName: donor.DisplayName,
            LifetimeDonated: lifetimeDonated,
            MonthlyHistory: monthlyHistory,
            ActiveCampaign: campaignSummary,
            Impact: impactSummary));
    }

    /// <summary>
    /// Records a new donation for the donor identified by the email in the request body.
    /// After a successful response, the frontend re-fetches GET /api/donor/dashboard
    /// so all metrics update to include this new contribution.
    /// </summary>
    [HttpPost("contribution")]
    public ActionResult<NewContributionResponse> AddContribution([FromBody] NewContributionRequest request)
    {
        // Validate required fields.
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = "Email is required." });

        if (request.Amount <= 0)
            return BadRequest(new { error = "Amount must be greater than zero." });

        // Look up the donor by email.
        var donor = store.Donors.FirstOrDefault(
            d => string.Equals(d.Email, request.Email, StringComparison.OrdinalIgnoreCase));

        if (donor is null)
            return NotFound(new { error = $"No donor found with email '{request.Email}'." });

        // Resolve the campaign: use the requested campaign ID, or auto-assign to the active campaign.
        Guid? resolvedCampaignId = request.CampaignId;
        if (resolvedCampaignId is null)
        {
            // Auto-assign to the currently active campaign if one exists.
            var activeCampaign = store.Campaigns
                .FirstOrDefault(c => c.StatusStateId == ActiveCampaignStatusId);
            resolvedCampaignId = activeCampaign?.Id;
        }

        // Create and store the new contribution.
        var contribution = new Contribution
        {
            Id = Guid.NewGuid(),
            DonorId = donor.Id,
            CampaignId = resolvedCampaignId,
            Amount = request.Amount,
            ContributionDate = DateTimeOffset.UtcNow,
            ContributionTypeId = OnlineDonationTypeId,
            StatusStateId = CompletedContributionStatusId, // Treat all donor-submitted donations as completed.
        };

        store.Contributions.Add(contribution);

        // Update the donor's lifetime total on the Donor model as well (denormalized field).
        donor.LifetimeDonations += request.Amount;
        donor.UpdatedAtUtc = DateTimeOffset.UtcNow;

        return CreatedAtAction(
            nameof(GetDashboard),
            new { email = request.Email },
            new NewContributionResponse(contribution.Id, "Thank you! Your donation has been added."));
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a 12-entry monthly history list covering the past 12 calendar months.
    /// Months with no contributions are zero-filled so the frontend bar chart always
    /// has a consistent 12-bar axis without needing to handle sparse data.
    /// </summary>
    private static IReadOnlyList<MonthlyDonationPoint> BuildMonthlyHistory(
        IEnumerable<Contribution> contributions)
    {
        // Group contributions by "yyyy-MM" key for easy lookup.
        var grouped = contributions
            .GroupBy(c => c.ContributionDate.ToString("yyyy-MM"))
            .ToDictionary(g => g.Key, g => g.Sum(c => c.Amount));

        var result = new List<MonthlyDonationPoint>(12);
        var reference = DateTimeOffset.UtcNow;

        // Walk backwards 11 months from today, then forward — gives ascending order.
        for (int i = 11; i >= 0; i--)
        {
            var month = reference.AddMonths(-i);
            var key = month.ToString("yyyy-MM");

            // Use the grouped amount, or 0 if no donations in that month.
            result.Add(new MonthlyDonationPoint(
                Month: key,
                Amount: grouped.TryGetValue(key, out var amount) ? amount : 0m));
        }

        return result;
    }

    /// <summary>
    /// Finds the active campaign and computes goal progress for the given donor.
    /// Returns null if no active campaign exists.
    /// </summary>
    private CampaignGoalSummary? BuildCampaignGoalSummary(Guid donorId)
    {
        var activeCampaign = store.Campaigns
            .FirstOrDefault(c => c.StatusStateId == ActiveCampaignStatusId);

        if (activeCampaign is null)
            return null;

        // Sum contributions across ALL donors for the campaign-wide progress bar.
        var totalRaisedAllDonors = store.Contributions
            .Where(c => c.CampaignId == activeCampaign.Id && c.StatusStateId == CompletedContributionStatusId)
            .Sum(c => c.Amount);

        // Sum only this donor's contributions for the "your share" line.
        var thisDonorContributed = store.Contributions
            .Where(c => c.CampaignId == activeCampaign.Id
                     && c.DonorId == donorId
                     && c.StatusStateId == CompletedContributionStatusId)
            .Sum(c => c.Amount);

        // Compute progress as a percentage, capped at 100 to handle over-funded campaigns.
        var progressPercent = activeCampaign.GoalAmount > 0
            ? Math.Min(100m, totalRaisedAllDonors / activeCampaign.GoalAmount * 100m)
            : 0m;

        return new CampaignGoalSummary(
            CampaignId: activeCampaign.Id,
            CampaignName: activeCampaign.Name,
            GoalAmount: activeCampaign.GoalAmount,
            TotalRaisedAllDonors: totalRaisedAllDonors,
            ThisDonorContributed: thisDonorContributed,
            ProgressPercent: Math.Round(progressPercent, 1));
    }
}
