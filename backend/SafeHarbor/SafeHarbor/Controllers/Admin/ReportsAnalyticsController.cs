using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using SafeHarbor.DTOs;
using SafeHarbor.Services.Admin;

namespace SafeHarbor.Controllers.Admin;

[ApiController]
[Route("api/admin/reports-analytics")]
[Authorize(Policy = PolicyNames.StaffOrAdmin)]
public sealed class ReportsAnalyticsController(IReportsAnalyticsService reportsAnalyticsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ReportsAnalyticsResponse>> Get(CancellationToken ct)
    {
        return Ok(await reportsAnalyticsService.GetAsync(ct));
    }
}
