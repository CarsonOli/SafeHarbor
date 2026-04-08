using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using SafeHarbor.DTOs;

namespace SafeHarbor.Controllers.Admin;

[ApiController]
[Route("api/admin/donors-contributions")]
[Authorize(Policy = PolicyNames.StaffOrAdmin)]
public sealed class DonorsContributionsController : ControllerBase
{
    [HttpGet("donors")]
    public ActionResult<PagedResult<DonorListItem>> GetDonors([FromQuery] PagingQuery query)
    {
        // TODO: Replace placeholder with donor records from IDonorFundingStore when database wiring is available.
        return Ok(new PagedResult<DonorListItem>(Array.Empty<DonorListItem>(), query.NormalizedPage, query.NormalizedPageSize, 0));
    }

    [HttpPost("donors")]
    public ActionResult<DonorListItem> CreateDonor([FromBody] CreateDonorRequest _)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, "Donor writes require database integration.");
    }

    [HttpPost("contributions")]
    public ActionResult<ContributionListItem> LogContribution([FromBody] CreateContributionRequest _)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, "Contribution writes require database integration.");
    }

    [HttpPost("allocations")]
    public IActionResult TrackAllocation([FromBody] CreateAllocationRequest _)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, "Allocation writes require database integration.");
    }
}
