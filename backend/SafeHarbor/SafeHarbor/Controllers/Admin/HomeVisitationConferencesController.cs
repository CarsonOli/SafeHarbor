using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeHarbor.Data;
using SafeHarbor.DTOs;

namespace SafeHarbor.Controllers.Admin;

[ApiController]
[Route("api/admin/visitation-conferences")]
[Authorize]
public sealed class HomeVisitationConferencesController(SafeHarborDbContext dbContext) : ControllerBase
{
    [HttpGet("visits")]
    public async Task<ActionResult<PagedResult<HomeVisitItem>>> GetVisitLogs(
        [FromQuery] PagingQuery query,
        [FromQuery] Guid? residentCaseId,
        CancellationToken cancellationToken)
    {
        var visits = dbContext.HomeVisits
            .AsNoTracking()
            .Include(x => x.VisitType)
            .Include(x => x.StatusState)
            .AsQueryable();

        if (residentCaseId.HasValue)
        {
            visits = visits.Where(x => x.ResidentCaseId == residentCaseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            visits = visits.Where(x => x.Notes.Contains(query.Search));
        }

        visits = query.Desc ? visits.OrderByDescending(x => x.VisitDate) : visits.OrderBy(x => x.VisitDate);

        var totalCount = await visits.CountAsync(cancellationToken);
        var page = query.NormalizedPage;
        var pageSize = query.NormalizedPageSize;

        var items = await visits
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new HomeVisitItem(
                x.Id,
                x.ResidentCaseId,
                x.VisitDate,
                x.VisitType != null ? x.VisitType.Name : "Unknown",
                x.StatusState != null ? x.StatusState.Name : "Unknown",
                x.Notes))
            .ToArrayAsync(cancellationToken);

        return Ok(new PagedResult<HomeVisitItem>(items, page, pageSize, totalCount));
    }

    [HttpGet("conferences/upcoming")]
    public Task<ActionResult<PagedResult<CaseConferenceItem>>> GetUpcoming([FromQuery] PagingQuery query, CancellationToken cancellationToken) =>
        GetConferences(query, true, cancellationToken);

    [HttpGet("conferences/previous")]
    public Task<ActionResult<PagedResult<CaseConferenceItem>>> GetPrevious([FromQuery] PagingQuery query, CancellationToken cancellationToken) =>
        GetConferences(query, false, cancellationToken);

    private async Task<ActionResult<PagedResult<CaseConferenceItem>>> GetConferences(PagingQuery query, bool upcoming, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var conferences = dbContext.CaseConferences
            .AsNoTracking()
            .Include(x => x.StatusState)
            .Where(x => upcoming ? x.ConferenceDate >= now : x.ConferenceDate < now)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            conferences = conferences.Where(x => x.OutcomeSummary.Contains(query.Search));
        }

        conferences = upcoming
            ? conferences.OrderBy(x => x.ConferenceDate)
            : conferences.OrderByDescending(x => x.ConferenceDate);

        var totalCount = await conferences.CountAsync(cancellationToken);
        var page = query.NormalizedPage;
        var pageSize = query.NormalizedPageSize;

        var items = await conferences
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CaseConferenceItem(
                x.Id,
                x.ResidentCaseId,
                x.ConferenceDate,
                x.StatusState != null ? x.StatusState.Name : "Unknown",
                x.OutcomeSummary))
            .ToArrayAsync(cancellationToken);

        return Ok(new PagedResult<CaseConferenceItem>(items, page, pageSize, totalCount));
    }
}
