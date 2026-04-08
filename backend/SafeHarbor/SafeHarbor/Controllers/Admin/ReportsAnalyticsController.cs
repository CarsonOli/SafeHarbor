using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using SafeHarbor.DTOs;

namespace SafeHarbor.Controllers.Admin;

[ApiController]
[Route("api/admin/reports-analytics")]
[Authorize(Policy = PolicyNames.StaffOrAdmin)]
public sealed class ReportsAnalyticsController : ControllerBase
{
    [HttpGet]
    public ActionResult<ReportsAnalyticsResponse> Get()
    {
        // TODO: Source these report sections from IReportingAnalyticsStore after database integration.
        // Returning stable empty sections keeps the contract predictable for front-end development.
        var response = new ReportsAnalyticsResponse(
            DonationTrends: Array.Empty<DonationTrendPoint>(),
            OutcomeTrends: Array.Empty<OutcomeTrendPoint>(),
            SafehouseComparisons: Array.Empty<SafehouseComparisonItem>(),
            ReintegrationRates: Array.Empty<ReintegrationRatePoint>(),
            DonationCorrelationByPlatform: Array.Empty<SocialDonationCorrelationPoint>(),
            DonationCorrelationByContentType: Array.Empty<SocialDonationCorrelationPoint>(),
            DonationCorrelationByPostingHour: Array.Empty<SocialDonationCorrelationPoint>(),
            TopAttributedPosts: Array.Empty<SocialPostDonationInsight>(),
            Recommendations:
            [
                new ContentTimingRecommendationCard(
                    "Connect analytics datastore",
                    "No persistent analytics data is available in the current local build.",
                    "Implement IReportingAnalyticsStore and map it to the production database provider.")
            ]);

        return Ok(response);
    }
}
