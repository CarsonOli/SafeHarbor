using System.Security.Claims;
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
///   GET  /api/donor/dashboard                — Returns full dashboard data for the authenticated donor.
///   POST /api/donor/contribution              — Records a new donation for a donor.
///
/// AUTHENTICATION NOTE:
///   This endpoint now requires an authenticated principal via PolicyNames.AuthenticatedUser
///   so donor routes no longer run anonymously.
///   TODO: Tighten to PolicyNames.DonorOnly once Microsoft Entra ID role claims are wired.
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
    /// Returns the full donor dashboard for the authenticated donor.
    ///
    /// The response includes lifetime totals, 12-month history, campaign goal progress,
    /// and an impact score (girls helped) calculated by the injected IDonorImpactCalculator.
    /// </summary>
    [HttpGet("dashboard")]
    public ActionResult<DonorDashboardResponse> GetDashboard([FromQuery] string? email = null)
    {
        // NOTE: `email` is intentionally ignored for security and backward compatibility.
        // The route previously trusted caller-supplied email which allowed horizontal access to
        // another donor's data. We now scope all donor reads to authenticated identity claims.
        var donorResolution = TryResolveAuthenticatedDonor();
        if (donorResolution.ErrorResult is not null)
            return donorResolution.ErrorResult;

        var donor = donorResolution.Donor!;

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
    /// Records a new donation for the authenticated donor.
    /// After a successful response, the frontend re-fetches GET /api/donor/dashboard
    /// so all metrics update to include this new contribution.
    /// </summary>
    [HttpPost("contribution")]
    public ActionResult<NewContributionResponse> AddContribution([FromBody] NewContributionRequest request)
    {
        if (request.Amount <= 0)
            return BadRequest(new { error = "Amount must be greater than zero." });

        var donorResolution = TryResolveAuthenticatedDonor();
        if (donorResolution.ErrorResult is not null)
            return donorResolution.ErrorResult;

        var donor = donorResolution.Donor!;

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
            null,
            new NewContributionResponse(contribution.Id, "Thank you! Your donation has been added."));
    }


    /// <summary>
    /// Resolves donor scope from authenticated claims only (never from query/body values).
    /// </summary>
    /// <remarks>
    /// Claim-based scoping is required to prevent horizontal data exposure where one donor could
    /// submit another donor's email address and read or mutate data outside their own account.
    /// </remarks>
    private (Donor? Donor, ActionResult? ErrorResult) TryResolveAuthenticatedDonor()
    {
        var email = User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue("emails")
            ?? User.FindFirstValue("preferred_username");

        var objectIdValue = User.FindFirstValue("oid")
            ?? User.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        Donor? donor = null;

        // Prefer object identifier when present because it is stable even when email changes.
        if (!string.IsNullOrWhiteSpace(objectIdValue) && Guid.TryParse(objectIdValue, out var donorId))
        {
            donor = store.Donors.FirstOrDefault(d => d.Id == donorId);
        }

        // Fall back to email matching for identity providers that do not emit object IDs.
        if (donor is null && !string.IsNullOrWhiteSpace(email))
        {
            donor = store.Donors.FirstOrDefault(
                d => string.Equals(d.Email, email, StringComparison.OrdinalIgnoreCase));
        }

        if (donor is not null)
            return (donor, null);

        // Return 403 when we cannot determine caller identity from claims at all.
        if (string.IsNullOrWhiteSpace(objectIdValue) && string.IsNullOrWhiteSpace(email))
            return (null, StatusCode(StatusCodes.Status403Forbidden, new { error = "Authenticated donor identity claim is required." }));

        // Return 404 when claims are present but do not map to a donor in our store.
        return (null, NotFound(new { error = "No donor profile found for the authenticated identity." }));
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
