using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using SafeHarbor.DTOs;
using SafeHarbor.Services.Admin;

namespace SafeHarbor.Controllers.Admin;

[ApiController]
[Route("api/admin/caseload")]
[Authorize(Policy = PolicyNames.StaffOrAdmin)]
public sealed class CaseloadInventoryController(ICaseloadInventoryService caseloadService) : ControllerBase
{
    [HttpGet("lookups")]
    public async Task<ActionResult<CaseloadLookupsResponse>> GetLookups(CancellationToken ct)
        => Ok(await caseloadService.GetLookupsAsync(ct));

    [HttpGet("residents")]
    public async Task<ActionResult<PagedResult<ResidentCaseListItem>>> GetResidents([FromQuery] PagingQuery query, CancellationToken ct)
    {
        return Ok(await caseloadService.GetResidentsAsync(query, ct));
    }

    [HttpPost("residents")]
    public async Task<ActionResult<ResidentCaseListItem>> CreateResidentCase([FromBody] CreateResidentCaseRequest request, CancellationToken ct)
    {
        var created = await caseloadService.CreateResidentCaseAsync(request, ct);
        return CreatedAtAction(nameof(GetResidents), new { id = created.Id }, created);
    }

    [HttpPut("residents/{id:guid}")]
    public async Task<ActionResult<ResidentCaseListItem>> UpdateResidentCase(Guid id, [FromBody] UpdateResidentCaseRequest request, CancellationToken ct)
    {
        var updated = await caseloadService.UpdateResidentCaseAsync(id, request, ct);
        if (updated is null)
        {
            return NotFound(new ApiErrorEnvelope("NotFound", $"Resident case {id} was not found.", HttpContext.TraceIdentifier));
        }

        return Ok(updated);
    }

    [HttpDelete("residents/{id:guid}")]
    public async Task<IActionResult> DeleteResidentCase(Guid id, CancellationToken ct)
    {
        var deleted = await caseloadService.DeleteResidentCaseAsync(id, ct);
        if (!deleted)
        {
            return NotFound(new ApiErrorEnvelope("NotFound", $"Resident case {id} was not found.", HttpContext.TraceIdentifier));
        }

        return NoContent();
    }
}
