using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using SafeHarbor.DTOs;
using SafeHarbor.Services;

namespace SafeHarbor.Controllers.Admin;

[ApiController]
[Route("api/admin/donors")]
[Authorize(Policy = PolicyNames.AdminOnly)]
public sealed class AdminDonorsController(IDonorAdminService donorAdminService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<DonorAdminResponse>>> GetAll(CancellationToken ct)
    {
        var payload = await donorAdminService.GetAllAsync(ct);
        return Ok(payload);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DonorAdminResponse>> GetById(Guid id, CancellationToken ct)
    {
        var donor = await donorAdminService.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException();
        return Ok(donor);
    }

    [HttpPost]
    public async Task<ActionResult<DonorAdminResponse>> Create([FromBody] DonorCreateRequest request, CancellationToken ct)
    {
        var donor = await donorAdminService.CreateAsync(request, User.Identity?.Name ?? "system", ct);
        return CreatedAtAction(nameof(GetById), new { id = donor.Id }, donor);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DonorAdminResponse>> Update(Guid id, [FromBody] DonorUpdateRequest request, CancellationToken ct)
    {
        var donor = await donorAdminService.UpdateAsync(id, request, User.Identity?.Name ?? "system", ct) ?? throw new KeyNotFoundException();
        return Ok(donor);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await donorAdminService.DeleteAsync(id, User.Identity?.Name ?? "system", ct);
        if (!deleted) throw new KeyNotFoundException();
        return NoContent();
    }

    [HttpGet("reports/summary")]
    public async Task<ActionResult<IReadOnlyCollection<DonorPublicResponse>>> ReportSummary(CancellationToken ct)
    {
        var payload = await donorAdminService.ReportSummaryAsync(ct);
        return Ok(payload);
    }
}
