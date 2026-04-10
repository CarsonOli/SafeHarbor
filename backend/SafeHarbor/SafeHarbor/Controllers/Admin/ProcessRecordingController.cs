using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using SafeHarbor.DTOs;
using SafeHarbor.Services.Admin;

namespace SafeHarbor.Controllers.Admin;

[ApiController]
[Route("api/admin/process-recordings")]
[Authorize(Policy = PolicyNames.StaffOrAdmin)]
public sealed class ProcessRecordingController(IProcessRecordingService processRecordingService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProcessRecordItem>>> GetByResidentCase([FromQuery] PagingQuery query, CancellationToken ct)
    {
        return Ok(await processRecordingService.GetAsync(query, ct));
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.StaffOrAdmin)]
    public async Task<ActionResult<ProcessRecordItem>> Create([FromBody] CreateProcessRecordRequest request, CancellationToken ct)
    {
        var created = await processRecordingService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetByResidentCase), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PolicyNames.StaffOrAdmin)]
    public async Task<ActionResult<ProcessRecordItem>> Update(Guid id, [FromBody] CreateProcessRecordRequest request, CancellationToken ct)
    {
        var updated = await processRecordingService.UpdateAsync(id, request, ct);
        if (updated is null)
        {
            return NotFound(new ApiErrorEnvelope("NotFound", $"Process recording {id} was not found.", HttpContext.TraceIdentifier));
        }

        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PolicyNames.StaffOrAdmin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await processRecordingService.DeleteAsync(id, ct);
        if (!deleted)
        {
            return NotFound(new ApiErrorEnvelope("NotFound", $"Process recording {id} was not found.", HttpContext.TraceIdentifier));
        }

        return NoContent();
    }
}
