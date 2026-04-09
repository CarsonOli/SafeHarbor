using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.DTOs;
using SafeHarbor.Services.Public;

namespace SafeHarbor.Controllers.Public;

[ApiController]
[Route("api/impact")]
[AllowAnonymous]
public sealed class ImpactController(IImpactAggregateService impactAggregateService) : ControllerBase
{
    [HttpGet("aggregate")]
    public async Task<ActionResult<ImpactSummaryDto>> GetAggregate(CancellationToken ct)
    {
        var response = await impactAggregateService.GetAggregateAsync(ct);
        return Ok(response);
    }
}
