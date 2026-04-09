using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using SafeHarbor.DTOs;
using SafeHarbor.Services;

namespace SafeHarbor.Controllers.Donor;

[ApiController]
[Route("api/donor")]
[Authorize(Policy = PolicyNames.DonorOnly)]
public sealed class DonorDashboardController(IDonorDashboardService donorDashboardService) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<DonorDashboardResponse>> GetDashboard([FromQuery] string? email = null, CancellationToken ct = default)
    {
        _ = email;

        var (donorId, donorEmail) = ResolveIdentityClaims();
        if (donorId is null && string.IsNullOrWhiteSpace(donorEmail))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Authenticated donor identity claim is required." });
        }

        var dashboard = await donorDashboardService.GetDashboardAsync(donorId, donorEmail, ct);
        if (dashboard is null)
        {
            return NotFound(new { error = "No donor profile found for the authenticated identity." });
        }

        return Ok(dashboard);
    }

    [HttpPost("contribution")]
    public async Task<ActionResult<NewContributionResponse>> AddContribution([FromBody] NewContributionRequest request, CancellationToken ct)
    {
        if (request.Amount <= 0)
            return BadRequest(new { error = "Amount must be greater than zero." });

        var (donorId, donorEmail) = ResolveIdentityClaims();
        if (donorId is null && string.IsNullOrWhiteSpace(donorEmail))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Authenticated donor identity claim is required." });
        }

        var contribution = await donorDashboardService.AddContributionAsync(donorId, donorEmail, request, ct);
        if (contribution is null)
        {
            return NotFound(new { error = "No donor profile found for the authenticated identity." });
        }

        return CreatedAtAction(nameof(GetDashboard), null, contribution);
    }

    private (Guid? donorId, string? donorEmail) ResolveIdentityClaims()
    {
        var email = User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue("emails")
            ?? User.FindFirstValue("preferred_username");

        var objectIdValue = User.FindFirstValue("oid")
            ?? User.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        // Explicit cast to nullable keeps the intent clear while avoiding Guid vs null inference issues.
        var donorId = Guid.TryParse(objectIdValue, out var parsedDonorId) ? (Guid?)parsedDonorId : null;
        return (donorId, email);
    }
}
