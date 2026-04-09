using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using SafeHarbor.DTOs;
using SafeHarbor.Services;

namespace SafeHarbor.Controllers.Admin;

[ApiController]
[Route("api/admin/residents")]
[Authorize(Policy = PolicyNames.AdminOnly)]
public sealed class AdminResidentsController(IResidentAdminService residentAdminService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ResidentAdminResponse>>> GetAll(CancellationToken ct)
    {
        var payload = await residentAdminService.GetAllAsync(ct);
        return Ok(payload);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ResidentAdminResponse>> GetById(Guid id, CancellationToken ct)
    {
        var resident = await residentAdminService.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException();
        return Ok(resident);
    }

    [HttpPost]
    public async Task<ActionResult<ResidentAdminResponse>> Create([FromBody] ResidentCreateRequest request, CancellationToken ct)
    {
        var resident = await residentAdminService.CreateAsync(request, User.Identity?.Name ?? "system", ct);
        return CreatedAtAction(nameof(GetById), new { id = resident.Id }, resident);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ResidentAdminResponse>> Update(Guid id, [FromBody] ResidentUpdateRequest request, CancellationToken ct)
    {
        var resident = await residentAdminService.UpdateAsync(id, request, User.Identity?.Name ?? "system", ct) ?? throw new KeyNotFoundException();
        return Ok(resident);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await residentAdminService.DeleteAsync(id, User.Identity?.Name ?? "system", ct);
        if (!deleted) throw new KeyNotFoundException();
        return NoContent();
    }

    [HttpGet("exports/snapshot")]
    public async Task<ActionResult<IReadOnlyCollection<ResidentAdminResponse>>> ExportSnapshot(CancellationToken ct)
    {
        var payload = await residentAdminService.ExportSnapshotAsync(ct);
        return Ok(payload);
    }
}
