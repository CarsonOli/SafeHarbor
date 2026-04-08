using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using SafeHarbor.DTOs;
using SafeHarbor.Infrastructure;
using SafeHarbor.Services;
using SafeHarbor.Models.Entities;

namespace SafeHarbor.Controllers.Admin;

[ApiController]
[Route("api/admin/donors")]
[Authorize(Policy = PolicyNames.AdminOnly)]
public sealed class AdminDonorsController(
    InMemoryDataStore store,
    IAuditLogger auditLogger,
    IDataRetentionRedactionService retentionRedactionService) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyCollection<DonorAdminResponse>> GetAll()
    {
        var payload = store.Donors.Select(MapAdmin).ToArray();
        return Ok(payload);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<DonorAdminResponse> GetById(Guid id)
    {
        var donor = store.Donors.FirstOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException();
        return Ok(MapAdmin(donor));
    }

    [HttpPost]
    public ActionResult<DonorAdminResponse> Create([FromBody] DonorCreateRequest request)
    {
        var donor = new SafeHarbor.Models.Entities.Donor
        {
            DisplayName = request.DisplayName,
            Email = request.Email,
            LifetimeDonations = request.LifetimeDonations,
            PaymentToken = request.PaymentToken,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        store.Donors.Add(donor);
        auditLogger.RecordMutation("donor", "create", donor.Id, User.Identity?.Name ?? "system");
        return CreatedAtAction(nameof(GetById), new { id = donor.Id }, MapAdmin(donor));
    }

    [HttpPut("{id:guid}")]
    public ActionResult<DonorAdminResponse> Update(Guid id, [FromBody] DonorUpdateRequest request)
    {
        var donor = store.Donors.FirstOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException();

        donor.DisplayName = request.DisplayName;
        donor.Email = request.Email;
        donor.LifetimeDonations = request.LifetimeDonations;
        donor.PaymentToken = request.PaymentToken;
        donor.UpdatedAtUtc = DateTimeOffset.UtcNow;

        auditLogger.RecordMutation("donor", "update", donor.Id, User.Identity?.Name ?? "system");
        return Ok(MapAdmin(donor));
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        var donor = store.Donors.FirstOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException();
        store.Donors.Remove(donor);
        auditLogger.RecordMutation("donor", "delete", donor.Id, User.Identity?.Name ?? "system");
        return NoContent();
    }

    [HttpGet("reports/summary")]
    public ActionResult<IReadOnlyCollection<DonorPublicResponse>> ReportSummary()
    {
        // NOTE: Keep summary report output detached from persistence models so sensitive fields cannot leak.
        var payload = store.Donors
            .Select(x => new DonorPublicResponse(x.Id, x.DisplayName, x.LifetimeDonations))
            .ToArray();

        return Ok(retentionRedactionService.ApplyRetentionPolicy(payload, "donor_summary_report"));
    }

    private static DonorAdminResponse MapAdmin(SafeHarbor.Models.Entities.Donor donor) =>
        new(
            donor.Id,
            donor.DisplayName,
            donor.Email,
            donor.LifetimeDonations,
            donor.PaymentToken,
            donor.CreatedAtUtc,
            donor.UpdatedAtUtc);
}
