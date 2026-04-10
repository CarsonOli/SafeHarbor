using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using SafeHarbor.DTOs;
using SafeHarbor.Services.Admin;

namespace SafeHarbor.Controllers.Admin;

[ApiController]
[Route("api/admin/visitation-conferences")]
[Authorize(Policy = PolicyNames.StaffOrAdmin)]
public sealed class HomeVisitationConferencesController(IVisitationConferenceService visitationConferenceService) : ControllerBase
{
    [HttpGet("visits")]
    public async Task<ActionResult<PagedResult<HomeVisitItem>>> GetVisitLogs([FromQuery] PagingQuery query, CancellationToken ct)
    {
        return Ok(await visitationConferenceService.GetVisitsAsync(query, ct));
    }

    [HttpGet("conferences/upcoming")]
    public async Task<ActionResult<PagedResult<CaseConferenceItem>>> GetUpcoming([FromQuery] PagingQuery query, CancellationToken ct)
    {
        return Ok(await visitationConferenceService.GetUpcomingAsync(query, ct));
    }

    [HttpGet("conferences/previous")]
    public async Task<ActionResult<PagedResult<CaseConferenceItem>>> GetPrevious([FromQuery] PagingQuery query, CancellationToken ct)
    {
        return Ok(await visitationConferenceService.GetPreviousAsync(query, ct));
    }

    [HttpPost("visits")]
    public async Task<ActionResult<HomeVisitItem>> LogVisit([FromBody] CreateHomeVisitRequest request, CancellationToken ct)
    {
        var item = await visitationConferenceService.CreateVisitAsync(request, ct);
        return CreatedAtAction(nameof(GetVisitLogs), item);
    }

    [HttpPost("conferences")]
    public async Task<ActionResult<CaseConferenceItem>> LogConference([FromBody] CreateCaseConferenceRequest request, CancellationToken ct)
    {
        var item = await visitationConferenceService.CreateConferenceAsync(request, ct);
        return CreatedAtAction(nameof(GetUpcoming), item);
    }
}
