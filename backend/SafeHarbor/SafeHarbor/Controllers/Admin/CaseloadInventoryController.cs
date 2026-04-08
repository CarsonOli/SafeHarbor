using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using SafeHarbor.DTOs;

namespace SafeHarbor.Controllers.Admin;

[ApiController]
[Route("api/admin/caseload")]
[Authorize(Policy = PolicyNames.StaffOrAdmin)]
public sealed class CaseloadInventoryController : ControllerBase
{
    [HttpGet("residents")]
    public ActionResult<PagedResult<ResidentCaseListItem>> GetResidents([FromQuery] PagingQuery query)
    {
        // TODO: Hydrate this list from ICaseManagementStore after database integration lands.
        return Ok(new PagedResult<ResidentCaseListItem>(Array.Empty<ResidentCaseListItem>(), query.NormalizedPage, query.NormalizedPageSize, 0));
    }

    [HttpPost("residents")]
    public ActionResult<ResidentCaseListItem> CreateResidentCase([FromBody] CreateResidentCaseRequest _)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, "Resident case writes require database integration.");
    }

    [HttpPut("residents/{id:guid}")]
    public ActionResult<ResidentCaseListItem> UpdateResidentCase(Guid id, [FromBody] UpdateResidentCaseRequest _)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, $"Resident case {id} cannot be updated until database integration is complete.");
    }

    [HttpDelete("residents/{id:guid}")]
    public IActionResult DeleteResidentCase(Guid id)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, $"Resident case {id} cannot be deleted until database integration is complete.");
    }
}
