using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeHarbor.Data;
using SafeHarbor.DTOs;
using SafeHarbor.Models.Entities;

namespace SafeHarbor.Controllers.Admin;

[ApiController]
[Route("api/admin/caseload")]
[Authorize]
public sealed class CaseloadInventoryController(SafeHarborDbContext dbContext) : ControllerBase
{
    [HttpGet("residents")]
    public async Task<ActionResult<PagedResult<ResidentCaseListItem>>> GetResidents(
        [FromQuery] PagingQuery query,
        [FromQuery] int? statusStateId,
        [FromQuery] Guid? safehouseId,
        [FromQuery] int? caseCategoryId,
        [FromQuery] string? socialWorkerExternalId,
        CancellationToken cancellationToken)
    {
        var residentCases = dbContext.ResidentCases
            .AsNoTracking()
            .Include(x => x.Safehouse)
            .Include(x => x.CaseCategory)
            .Include(x => x.StatusState)
            .Include(x => x.ResidentUser)
            .AsQueryable();

        if (statusStateId.HasValue)
        {
            residentCases = residentCases.Where(x => x.StatusStateId == statusStateId.Value);
        }

        if (safehouseId.HasValue)
        {
            residentCases = residentCases.Where(x => x.SafehouseId == safehouseId.Value);
        }

        if (caseCategoryId.HasValue)
        {
            residentCases = residentCases.Where(x => x.CaseCategoryId == caseCategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(socialWorkerExternalId))
        {
            residentCases = residentCases.Where(x => x.ResidentUser != null && x.ResidentUser.ExternalId == socialWorkerExternalId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            residentCases = residentCases.Where(x =>
                (x.Safehouse != null && x.Safehouse.Name.Contains(query.Search)) ||
                (x.CaseCategory != null && x.CaseCategory.Name.Contains(query.Search)) ||
                (x.ResidentUser != null && x.ResidentUser.DisplayName.Contains(query.Search)));
        }

        residentCases = (query.SortBy?.ToLowerInvariant(), query.Desc) switch
        {
            ("openedat", true) => residentCases.OrderByDescending(x => x.OpenedAt),
            ("openedat", false) => residentCases.OrderBy(x => x.OpenedAt),
            ("closedat", true) => residentCases.OrderByDescending(x => x.ClosedAt),
            ("closedat", false) => residentCases.OrderBy(x => x.ClosedAt),
            (_, true) => residentCases.OrderByDescending(x => x.UpdatedAt),
            _ => residentCases.OrderBy(x => x.UpdatedAt)
        };

        var totalCount = await residentCases.CountAsync(cancellationToken);
        var page = query.NormalizedPage;
        var pageSize = query.NormalizedPageSize;

        var items = await residentCases
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ResidentCaseListItem(
                x.Id,
                x.SafehouseId,
                x.Safehouse != null ? x.Safehouse.Name : "Unknown",
                x.CaseCategoryId,
                x.CaseCategory != null ? x.CaseCategory.Name : "Unknown",
                x.StatusStateId,
                x.StatusState != null ? x.StatusState.Name : "Unknown",
                x.ResidentUser != null ? x.ResidentUser.ExternalId : null,
                x.OpenedAt,
                x.ClosedAt))
            .ToArrayAsync(cancellationToken);

        return Ok(new PagedResult<ResidentCaseListItem>(items, page, pageSize, totalCount));
    }

    [HttpPost("residents")]
    public async Task<ActionResult<ResidentCaseListItem>> CreateResidentCase([FromBody] CreateResidentCaseRequest request, CancellationToken cancellationToken)
    {
        var residentCase = new ResidentCase
        {
            Id = Guid.NewGuid(),
            SafehouseId = request.SafehouseId,
            CaseCategoryId = request.CaseCategoryId,
            CaseSubcategoryId = request.CaseSubcategoryId,
            StatusStateId = request.StatusStateId,
            ResidentUserId = request.ResidentUserId,
            OpenedAt = request.OpenedAt ?? DateTimeOffset.UtcNow
        };

        dbContext.ResidentCases.Add(residentCase);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetResidents), new { residentCase.Id }, await MapResidentCase(residentCase.Id, cancellationToken));
    }

    [HttpPut("residents/{id:guid}")]
    public async Task<ActionResult<ResidentCaseListItem>> UpdateResidentCase(Guid id, [FromBody] UpdateResidentCaseRequest request, CancellationToken cancellationToken)
    {
        var residentCase = await dbContext.ResidentCases.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new KeyNotFoundException("Resident case not found.");

        residentCase.SafehouseId = request.SafehouseId;
        residentCase.CaseCategoryId = request.CaseCategoryId;
        residentCase.CaseSubcategoryId = request.CaseSubcategoryId;
        residentCase.StatusStateId = request.StatusStateId;
        residentCase.ResidentUserId = request.ResidentUserId;
        residentCase.ClosedAt = request.ClosedAt;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(await MapResidentCase(id, cancellationToken));
    }

    [HttpDelete("residents/{id:guid}")]
    public async Task<IActionResult> DeleteResidentCase(Guid id, CancellationToken cancellationToken)
    {
        var residentCase = await dbContext.ResidentCases.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new KeyNotFoundException("Resident case not found.");
        dbContext.ResidentCases.Remove(residentCase);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<ResidentCaseListItem> MapResidentCase(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.ResidentCases
            .AsNoTracking()
            .Include(x => x.Safehouse)
            .Include(x => x.CaseCategory)
            .Include(x => x.StatusState)
            .Include(x => x.ResidentUser)
            .Where(x => x.Id == id)
            .Select(x => new ResidentCaseListItem(
                x.Id,
                x.SafehouseId,
                x.Safehouse != null ? x.Safehouse.Name : "Unknown",
                x.CaseCategoryId,
                x.CaseCategory != null ? x.CaseCategory.Name : "Unknown",
                x.StatusStateId,
                x.StatusState != null ? x.StatusState.Name : "Unknown",
                x.ResidentUser != null ? x.ResidentUser.ExternalId : null,
                x.OpenedAt,
                x.ClosedAt))
            .FirstAsync(cancellationToken);
    }
}
