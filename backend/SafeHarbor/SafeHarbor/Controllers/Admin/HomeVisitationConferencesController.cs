using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using SafeHarbor.DTOs;

namespace SafeHarbor.Controllers.Admin;

[ApiController]
[Route("api/admin/visitation-conferences")]
[Authorize(Policy = PolicyNames.StaffOrAdmin)]
public sealed class HomeVisitationConferencesController : ControllerBase
{
    [HttpGet("visits")]
    public ActionResult<PagedResult<HomeVisitItem>> GetVisitLogs([FromQuery] PagingQuery query)
    {
        // TODO: Query IVisitationConferenceStore once real data persistence is connected.
        return Ok(new PagedResult<HomeVisitItem>(Array.Empty<HomeVisitItem>(), query.NormalizedPage, query.NormalizedPageSize, 0));
    }

    [HttpGet("conferences/upcoming")]
    public ActionResult<PagedResult<CaseConferenceItem>> GetUpcoming([FromQuery] PagingQuery query)
    {
        return Ok(new PagedResult<CaseConferenceItem>(Array.Empty<CaseConferenceItem>(), query.NormalizedPage, query.NormalizedPageSize, 0));
    }

    [HttpGet("conferences/previous")]
    public ActionResult<PagedResult<CaseConferenceItem>> GetPrevious([FromQuery] PagingQuery query)
    {
        return Ok(new PagedResult<CaseConferenceItem>(Array.Empty<CaseConferenceItem>(), query.NormalizedPage, query.NormalizedPageSize, 0));
    }
}
