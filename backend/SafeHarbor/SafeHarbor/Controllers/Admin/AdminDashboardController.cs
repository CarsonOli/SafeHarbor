using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using SafeHarbor.DTOs;
using SafeHarbor.Services.Admin;

namespace SafeHarbor.Controllers.Admin;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Policy = PolicyNames.StaffOrAdmin)]
public sealed class AdminDashboardController(IAdminDashboardService adminDashboardService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary(CancellationToken ct)
    {
        return Ok(await adminDashboardService.GetSummaryAsync(ct));
    }
}
