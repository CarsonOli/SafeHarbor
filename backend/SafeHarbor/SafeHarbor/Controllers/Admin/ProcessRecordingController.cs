using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using SafeHarbor.DTOs;

namespace SafeHarbor.Controllers.Admin;

[ApiController]
[Route("api/admin/process-recordings")]
[Authorize(Policy = PolicyNames.StaffOrAdmin)]
public sealed class ProcessRecordingController : ControllerBase
{
    [HttpGet]
    public ActionResult<PagedResult<ProcessRecordItem>> GetByResidentCase([FromQuery] Guid residentCaseId, [FromQuery] PagingQuery query)
    {
        // TODO: Fetch process recordings from ICaseNarrativeStore when persistence is available.
        // residentCaseId is intentionally kept in the contract so front-end integration does not change later.
        _ = residentCaseId;
        return Ok(new PagedResult<ProcessRecordItem>(Array.Empty<ProcessRecordItem>(), query.NormalizedPage, query.NormalizedPageSize, 0));
    }

    [HttpPost]
    // NOTE: Write operations stay SocialWorker-only as a stricter override because
    // process recordings contain sensitive case narrative details.
    [Authorize(Policy = PolicyNames.SocialWorkerOnly)]
    public ActionResult<ProcessRecordItem> Create([FromBody] CreateProcessRecordRequest _)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, "Process recording writes require database integration.");
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PolicyNames.SocialWorkerOnly)]
    public ActionResult<ProcessRecordItem> Update(Guid id, [FromBody] CreateProcessRecordRequest _)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, $"Process record {id} cannot be updated until database integration is complete.");
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PolicyNames.SocialWorkerOnly)]
    public IActionResult Delete(Guid id)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, $"Process record {id} cannot be deleted until database integration is complete.");
    }
}
