using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using SafeHarbor.DTOs;
using SafeHarbor.Services;

namespace SafeHarbor.Controllers.Admin;

/// <summary>
/// Provides aggregated donor analytics for the admin dashboard.
/// </summary>
[ApiController]
[Route("api/admin/donor-analytics")]
///[Authorize(Policy = PolicyNames.AdminOnly)]
public sealed class AdminDonorAnalyticsController(IDonorAnalyticsService donorAnalyticsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DonorAnalyticsResponse>> GetAnalytics(CancellationToken ct)
    {
        var payload = await donorAnalyticsService.GetAnalyticsAsync(ct);
        return Ok(payload);
    }
}
