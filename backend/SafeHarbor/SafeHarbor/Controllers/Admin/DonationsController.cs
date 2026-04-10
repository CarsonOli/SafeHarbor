using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using SafeHarbor.DTOs;
using SafeHarbor.Services.Donations;

namespace SafeHarbor.Controllers.Admin;

[ApiController]
[Route("api/admin/donations")]
[Authorize(Policy = PolicyNames.StaffOrAdmin)]
public sealed class DonationsController(IDonationAccessService donationAccessService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<DonationListItem>>> GetAll([FromQuery] DonationFiltersQuery filters, CancellationToken ct)
        => Ok(await donationAccessService.GetAllDonationsAsync(filters, ct));

    [HttpGet("{donationId:long}")]
    public async Task<ActionResult<DonationListItem>> GetById(long donationId, CancellationToken ct)
    {
        var item = await donationAccessService.GetDonationByIdAsync(donationId, ct);
        if (item is null)
        {
            return NotFound(new { error = "Donation not found." });
        }

        return Ok(item);
    }

    [HttpPost("link-user-supporter")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    public async Task<IActionResult> LinkUserToSupporter([FromBody] LinkUserToSupporterRequest request, CancellationToken ct)
    {
        var linked = await donationAccessService.LinkUserToSupporterAsync(request.UserId, request.SupporterId, ct);
        if (!linked)
        {
            return NotFound(new { error = "User or supporter record was not found." });
        }

        return NoContent();
    }
}
