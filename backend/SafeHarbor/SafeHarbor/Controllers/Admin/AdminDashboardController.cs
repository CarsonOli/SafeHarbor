using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using SafeHarbor.DTOs;

namespace SafeHarbor.Controllers.Admin;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Policy = PolicyNames.StaffOrAdmin)]
public sealed class AdminDashboardController : ControllerBase
{
    [HttpGet]
    public ActionResult<DashboardSummaryResponse> GetSummary()
    {
        // TODO: Replace these placeholders when IOperationalReportingStore is implemented.
        // The project is intentionally running without a live database until infrastructure is ready.
        var response = new DashboardSummaryResponse(
            ActiveResidents: 0,
            RecentContributions: Array.Empty<ContributionListItem>(),
            UpcomingConferences: Array.Empty<ConferenceListItem>(),
            SummaryOutcomes: Array.Empty<OutcomeSummaryItem>());

        return Ok(response);
    }
}
