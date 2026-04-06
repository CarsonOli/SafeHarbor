using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeHarbor.Data;
using SafeHarbor.DTOs;
using SafeHarbor.Models.Entities;

namespace SafeHarbor.Controllers.Admin;

[ApiController]
[Route("api/admin/process-recordings")]
[Authorize]
public sealed class ProcessRecordingController(SafeHarborDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProcessRecordItem>>> GetByResidentCase(
        [FromQuery] Guid residentCaseId,
        [FromQuery] PagingQuery query,
        CancellationToken cancellationToken)
    {
        var processQuery = dbContext.ProcessRecordings
            .AsNoTracking()
            .Where(x => x.ResidentCaseId == residentCaseId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            processQuery = processQuery.Where(x => x.Summary.Contains(query.Search));
        }

        processQuery = query.Desc
            ? processQuery.OrderByDescending(x => x.RecordedAt)
            : processQuery.OrderBy(x => x.RecordedAt);

        var totalCount = await processQuery.CountAsync(cancellationToken);
        var page = query.NormalizedPage;
        var pageSize = query.NormalizedPageSize;

        var items = await processQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ProcessRecordItem(x.Id, x.ResidentCaseId, x.RecordedAt, x.Summary))
            .ToArrayAsync(cancellationToken);

        return Ok(new PagedResult<ProcessRecordItem>(items, page, pageSize, totalCount));
    }

    // NOTE: Process-record writes are intentionally constrained to SocialWorker to enforce case-note stewardship.
    [HttpPost]
    [Authorize(Roles = "SocialWorker")]
    public async Task<ActionResult<ProcessRecordItem>> Create([FromBody] CreateProcessRecordRequest request, CancellationToken cancellationToken)
    {
        var record = new ProcessRecording
        {
            Id = Guid.NewGuid(),
            ResidentCaseId = request.ResidentCaseId,
            RecordedAt = request.RecordedAt ?? DateTimeOffset.UtcNow,
            Summary = request.Summary
        };

        dbContext.ProcessRecordings.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetByResidentCase), new { residentCaseId = request.ResidentCaseId }, new ProcessRecordItem(record.Id, record.ResidentCaseId, record.RecordedAt, record.Summary));
    }

    // NOTE: Role-based write control applies to updates and deletes for process recordings.
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SocialWorker")]
    public async Task<ActionResult<ProcessRecordItem>> Update(Guid id, [FromBody] CreateProcessRecordRequest request, CancellationToken cancellationToken)
    {
        var record = await dbContext.ProcessRecordings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new KeyNotFoundException("Process record not found.");

        record.ResidentCaseId = request.ResidentCaseId;
        record.RecordedAt = request.RecordedAt ?? record.RecordedAt;
        record.Summary = request.Summary;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new ProcessRecordItem(record.Id, record.ResidentCaseId, record.RecordedAt, record.Summary));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SocialWorker")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var record = await dbContext.ProcessRecordings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new KeyNotFoundException("Process record not found.");

        dbContext.ProcessRecordings.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
