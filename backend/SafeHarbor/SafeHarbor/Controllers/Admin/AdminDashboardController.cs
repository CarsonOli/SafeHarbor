using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeHarbor.Data;
using SafeHarbor.DTOs;
using SafeHarbor.Models.Enums;

namespace SafeHarbor.Controllers.Admin;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize]
public sealed class AdminDashboardController(SafeHarborDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary(CancellationToken cancellationToken)
    {
        var activeResidents = await dbContext.ResidentCases
            .Where(x => x.StatusState != null && x.StatusState.Domain == StatusDomain.ResidentCase && (x.StatusState.Code == "OPEN" || x.StatusState.Code == "ACTIVE"))
            .CountAsync(cancellationToken);

        var recentContributions = await dbContext.Contributions
            .AsNoTracking()
            .Include(x => x.Donor)
            .Include(x => x.StatusState)
            .OrderByDescending(x => x.ContributionDate)
            .Take(10)
            .Select(x => new ContributionListItem(
                x.Id,
                x.Donor != null ? x.Donor.Name : "Unknown donor",
                x.Amount,
                x.ContributionDate,
                x.StatusState != null ? x.StatusState.Name : "Unknown"))
            .ToArrayAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var upcomingConferences = await dbContext.CaseConferences
            .AsNoTracking()
            .Include(x => x.StatusState)
            .Where(x => x.ConferenceDate >= now)
            .OrderBy(x => x.ConferenceDate)
            .Take(10)
            .Select(x => new ConferenceListItem(
                x.Id,
                x.ResidentCaseId,
                x.ConferenceDate,
                x.StatusState != null ? x.StatusState.Name : "Unknown",
                x.OutcomeSummary))
            .ToArrayAsync(cancellationToken);

        var outcomeSummary = await dbContext.OutcomeSnapshots
            .AsNoTracking()
            .OrderByDescending(x => x.SnapshotDate)
            .Take(6)
            .Select(x => new OutcomeSummaryItem(x.SnapshotDate, x.TotalResidentsServed, x.TotalHomeVisits, x.TotalContributions))
            .ToArrayAsync(cancellationToken);

        return Ok(new DashboardSummaryResponse(activeResidents, recentContributions, upcomingConferences, outcomeSummary));
    }
}
