using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using SafeHarbor.DTOs;
using SafeHarbor.Infrastructure;
using SafeHarbor.Models;
using SafeHarbor.Services;

namespace SafeHarbor.Controllers.Admin;

[ApiController]
[Route("api/admin/residents")]
[Authorize(Policy = PolicyNames.AdminOnly)]
public sealed class AdminResidentsController(
    InMemoryDataStore store,
    IAuditLogger auditLogger,
    IDataRetentionRedactionService retentionRedactionService) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyCollection<ResidentAdminResponse>> GetAll()
    {
        var payload = store.Residents.Select(MapAdmin).ToArray();
        return Ok(payload);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<ResidentAdminResponse> GetById(Guid id)
    {
        var resident = store.Residents.FirstOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException();
        return Ok(MapAdmin(resident));
    }

    [HttpPost]
    public ActionResult<ResidentAdminResponse> Create([FromBody] ResidentCreateRequest request)
    {
        var resident = new Resident
        {
            FullName = request.FullName,
            DateOfBirth = request.DateOfBirth,
            CaseWorkerEmail = request.CaseWorkerEmail,
            MedicalNotes = request.MedicalNotes ?? string.Empty,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        store.Residents.Add(resident);
        auditLogger.RecordMutation("resident", "create", resident.Id, User.Identity?.Name ?? "system");
        return CreatedAtAction(nameof(GetById), new { id = resident.Id }, MapAdmin(resident));
    }

    [HttpPut("{id:guid}")]
    public ActionResult<ResidentAdminResponse> Update(Guid id, [FromBody] ResidentUpdateRequest request)
    {
        var resident = store.Residents.FirstOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException();

        resident.FullName = request.FullName;
        resident.DateOfBirth = request.DateOfBirth;
        resident.CaseWorkerEmail = request.CaseWorkerEmail;
        resident.MedicalNotes = request.MedicalNotes ?? string.Empty;
        resident.UpdatedAtUtc = DateTimeOffset.UtcNow;

        auditLogger.RecordMutation("resident", "update", resident.Id, User.Identity?.Name ?? "system");
        return Ok(MapAdmin(resident));
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        var resident = store.Residents.FirstOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException();
        store.Residents.Remove(resident);
        auditLogger.RecordMutation("resident", "delete", resident.Id, User.Identity?.Name ?? "system");
        return NoContent();
    }

    [HttpGet("exports/snapshot")]
    public ActionResult<IReadOnlyCollection<ResidentAdminResponse>> ExportSnapshot()
    {
        // NOTE: Use retention and redaction hooks here so export/report pathways can be hardened without API contract churn.
        var payload = store.Residents
            .Select(x => MapAdmin(x) with { MedicalNotes = retentionRedactionService.RedactFreeText(x.MedicalNotes) })
            .ToArray();

        return Ok(retentionRedactionService.ApplyRetentionPolicy(payload, "resident_export"));
    }

    private static ResidentAdminResponse MapAdmin(Resident resident) =>
        new(
            resident.Id,
            resident.FullName,
            resident.DateOfBirth,
            resident.CaseWorkerEmail,
            resident.MedicalNotes,
            resident.CreatedAtUtc,
            resident.UpdatedAtUtc);
}
