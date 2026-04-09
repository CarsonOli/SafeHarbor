using Microsoft.AspNetCore.Mvc;
using SafeHarbor.DTOs;
using SafeHarbor.Services;

namespace SafeHarbor.Controllers.Public;

[ApiController]
[Route("api/public")]
public sealed class PublicRecordsController(IPublicRecordsService publicRecordsService) : ControllerBase
{
    [HttpGet("residents")]
    public async Task<ActionResult<IReadOnlyCollection<ResidentPublicResponse>>> GetResidents(CancellationToken ct)
    {
        var residents = await publicRecordsService.GetResidentsAsync(ct);
        return Ok(residents);
    }

    [HttpGet("donors")]
    public async Task<ActionResult<IReadOnlyCollection<DonorPublicResponse>>> GetDonors(CancellationToken ct)
    {
        var donors = await publicRecordsService.GetDonorsAsync(ct);
        return Ok(donors);
    }
}
