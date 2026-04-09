using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using SafeHarbor.DTOs;
using SafeHarbor.Services.Admin;

namespace SafeHarbor.Controllers.Admin;

[ApiController]
[Route("api/admin/donors-contributions")]
[Authorize(Policy = PolicyNames.StaffOrAdmin)]
public sealed class DonorsContributionsController(IDonorContributionService donorContributionService) : ControllerBase
{
    [HttpGet("donors")]
    public async Task<ActionResult<PagedResult<DonorListItem>>> GetDonors([FromQuery] PagingQuery query, CancellationToken ct)
    {
        return Ok(await donorContributionService.GetDonorsAsync(query, ct));
    }

    [HttpPost("donors")]
    public async Task<ActionResult<DonorListItem>> CreateDonor([FromBody] CreateDonorRequest request, CancellationToken ct)
    {
        var created = await donorContributionService.CreateDonorAsync(request, ct);
        return CreatedAtAction(nameof(GetDonors), new { id = created.Id }, created);
    }

    [HttpPost("contributions")]
    public async Task<ActionResult<ContributionListItem>> LogContribution([FromBody] CreateContributionRequest request, CancellationToken ct)
    {
        var created = await donorContributionService.CreateContributionAsync(request, ct);
        return Ok(created);
    }

    [HttpPost("allocations")]
    public async Task<IActionResult> TrackAllocation([FromBody] CreateAllocationRequest request, CancellationToken ct)
    {
        var created = await donorContributionService.CreateAllocationAsync(request, ct);
        if (!created)
        {
            return NotFound(new ApiErrorEnvelope(
                "RelationshipNotFound",
                "Contribution and safehouse are required before an allocation can be recorded.",
                HttpContext.TraceIdentifier));
        }

        return NoContent();
    }
}
