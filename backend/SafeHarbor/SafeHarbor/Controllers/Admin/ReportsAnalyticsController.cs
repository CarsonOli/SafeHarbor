using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeHarbor.Data;
using SafeHarbor.DTOs;
using SafeHarbor.Models.Enums;

namespace SafeHarbor.Controllers.Admin;

[ApiController]
[Route("api/admin/reports-analytics")]
[Authorize]
public sealed class ReportsAnalyticsController(SafeHarborDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ReportsAnalyticsResponse>> Get(CancellationToken cancellationToken)
    {
        var donationTrends = await dbContext.Contributions
            .AsNoTracking()
            .GroupBy(x => new { x.ContributionDate.Year, x.ContributionDate.Month })
            .OrderBy(x => x.Key.Year)
            .ThenBy(x => x.Key.Month)
            .Select(x => new DonationTrendPoint($"{x.Key.Year}-{x.Key.Month:D2}", x.Sum(y => y.Amount)))
            .ToArrayAsync(cancellationToken);

        var outcomeTrends = await dbContext.OutcomeSnapshots
            .AsNoTracking()
            .OrderBy(x => x.SnapshotDate)
            .Select(x => new OutcomeTrendPoint(
                $"{x.SnapshotDate.Year}-{x.SnapshotDate.Month:D2}",
                x.TotalResidentsServed,
                x.TotalHomeVisits))
            .ToArrayAsync(cancellationToken);

        var activeResidentsBySafehouse = await dbContext.ResidentCases
            .AsNoTracking()
            .Where(x => x.StatusState != null && x.StatusState.Domain == StatusDomain.ResidentCase && (x.StatusState.Code == "OPEN" || x.StatusState.Code == "ACTIVE"))
            .GroupBy(x => x.SafehouseId)
            .Select(x => new { SafehouseId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.SafehouseId, x => x.Count, cancellationToken);

        var allocationBySafehouse = await dbContext.ContributionAllocations
            .AsNoTracking()
            .GroupBy(x => x.SafehouseId)
            .Select(x => new { SafehouseId = x.Key, Amount = x.Sum(y => y.AmountAllocated) })
            .ToDictionaryAsync(x => x.SafehouseId, x => x.Amount, cancellationToken);

        var safehouseComparisons = await dbContext.Safehouses
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new SafehouseComparisonItem(
                x.Name,
                activeResidentsBySafehouse.TryGetValue(x.Id, out var activeResidents) ? activeResidents : 0,
                allocationBySafehouse.TryGetValue(x.Id, out var allocatedFunding) ? allocatedFunding : 0))
            .ToArrayAsync(cancellationToken);

        var reintegrationRates = await dbContext.ResidentCases
            .AsNoTracking()
            .Where(x => x.ClosedAt != null)
            .GroupBy(x => new { Year = x.ClosedAt!.Value.Year, Month = x.ClosedAt!.Value.Month })
            .Select(x => new
            {
                x.Key.Year,
                x.Key.Month,
                Closed = x.Count(),
                Opened = dbContext.ResidentCases.Count(y => y.OpenedAt.Year == x.Key.Year && y.OpenedAt.Month == x.Key.Month)
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToArrayAsync(cancellationToken);

        var reintegrationRateItems = reintegrationRates
            .Select(x => new ReintegrationRatePoint($"{x.Year}-{x.Month:D2}", x.Closed / (decimal)Math.Max(1, x.Opened) * 100))
            .ToArray();

        return Ok(new ReportsAnalyticsResponse(donationTrends, outcomeTrends, safehouseComparisons, reintegrationRateItems));
    }
}
