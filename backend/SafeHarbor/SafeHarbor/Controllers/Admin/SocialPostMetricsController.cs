using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using SafeHarbor.DTOs;

namespace SafeHarbor.Controllers.Admin;

[ApiController]
[Route("api/admin/social-post-metrics")]
[Authorize(Policy = PolicyNames.StaffOrAdmin)]
public sealed class SocialPostMetricsController : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyCollection<SocialPostMetricListItem>> Get()
    {
        // TODO: Load from ISocialAttributionStore once the database-backed implementation is in place.
        return Ok(Array.Empty<SocialPostMetricListItem>());
    }

    [HttpPost]
    public ActionResult<SocialPostMetricListItem> Create([FromBody] CreateSocialPostMetricRequest _)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, "Social post metric writes require database integration.");
    }
}
