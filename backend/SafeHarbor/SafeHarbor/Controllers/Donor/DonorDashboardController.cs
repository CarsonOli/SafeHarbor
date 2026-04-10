using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using SafeHarbor.DTOs;
using SafeHarbor.Services;
using SafeHarbor.Services.Donations;

namespace SafeHarbor.Controllers.Donor;

[ApiController]
[Route("api/donor")]
[Authorize(Policy = PolicyNames.DonorOnly)]
public sealed class DonorDashboardController(
    IDonorDashboardService donorDashboardService,
    IDonationAccessService donationAccessService) : ControllerBase
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

        var userId = ResolveUserId();
        if (userId is null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Authenticated user id claim is required." });
        }

        var (_, donorEmail) = ResolveIdentityClaims();
        if (string.IsNullOrWhiteSpace(donorEmail))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Authenticated donor email claim is required." });
        }

        var supporterDonationId = await donationAccessService.CreateDonationForCurrentUserAsync(
            userId.Value,
            donorEmail,
            request.Amount,
            "One-time",
            ct);
        if (supporterDonationId is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Unable to record donation for this account." });
        }

        var syntheticId = ToDeterministicGuid(supporterDonationId.Value);
        return CreatedAtAction(nameof(GetDashboard), null, new NewContributionResponse(syntheticId, "Thank you! Your donation has been added."));
    }

    [HttpGet("donations")]
    public async Task<ActionResult<YourDonationsResponse>> GetCurrentUserDonations(CancellationToken ct)
        => await GetCurrentUserDonationsCore(ct);

    [HttpGet("your-donations")]
    public async Task<ActionResult<YourDonationsResponse>> GetCurrentUserDonationsLegacy(CancellationToken ct)
        => await GetCurrentUserDonationsCore(ct);

    [HttpGet("dashboard/donations")]
    public async Task<ActionResult<YourDonationsResponse>> GetCurrentUserDonationsDashboardScoped(CancellationToken ct)
        => await GetCurrentUserDonationsCore(ct);

    private async Task<ActionResult<YourDonationsResponse>> GetCurrentUserDonationsCore(CancellationToken ct)
    {
        var userId = ResolveUserId();
        if (userId is null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Authenticated user id claim is required." });
        }

        return Ok(await donationAccessService.GetCurrentUserDonationsAsync(userId.Value, ct));
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

    private Guid? ResolveUserId()
    {
        var userIdValue = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }

    private static Guid ToDeterministicGuid(long id)
    {
        // Keep response contract stable (Guid contribution id) when backing row id is bigint.
        return Guid.TryParseExact($"00000000-0000-0000-0000-{id:000000000000}", "D", out var parsed)
            ? parsed
            : Guid.NewGuid();
    }
}
