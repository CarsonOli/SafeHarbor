using Microsoft.EntityFrameworkCore;
using SafeHarbor.Data;
using SafeHarbor.DTOs;

namespace SafeHarbor.Services.Public;

public sealed class ImpactAggregateService(SafeHarborDbContext dbContext) : IImpactAggregateService
{
    public async Task<ImpactSummaryDto> GetAggregateAsync(CancellationToken ct)
    {
        var utcNow = DateTimeOffset.UtcNow;
        var currentMonth = new DateOnly(utcNow.Year, utcNow.Month, 1);
        var previousMonth = currentMonth.AddMonths(-1);

        var totalCases = await dbContext.ResidentCases.CountAsync(ct);

        var currentMonthCases = await dbContext.ResidentCases
            .Where(rc => rc.OpenedAt >= currentMonth.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                && rc.OpenedAt < currentMonth.AddMonths(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
            .CountAsync(ct);

        var previousMonthCases = await dbContext.ResidentCases
            .Where(rc => rc.OpenedAt >= previousMonth.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                && rc.OpenedAt < currentMonth.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
            .CountAsync(ct);

        var totalHomeVisits = await dbContext.HomeVisits.CountAsync(ct);
        var totalSafehouses = await dbContext.Safehouses.CountAsync(ct);

        // NOTE: Change percentages are computed only from aggregate counts and never from resident identifiers.
        // This preserves anonymization while still communicating momentum to public dashboard viewers.
        var metrics = new[]
        {
            new ImpactMetricDto("Households Supported", totalCases, CalculatePercentChange(currentMonthCases, previousMonthCases)),
            new ImpactMetricDto("Referrals Completed", totalHomeVisits, 0m),
            new ImpactMetricDto("Partner Programs Active", totalSafehouses, 0m),
        };

        var monthlyCaseCounts = await dbContext.ResidentCases
            .GroupBy(rc => new { rc.OpenedAt.Year, rc.OpenedAt.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Count = group.Count()
            })
            .ToListAsync(ct);

        var monthStarts = Enumerable.Range(0, 5)
            .Select(offset => currentMonth.AddMonths(-(4 - offset)))
            .ToArray();

        var monthlyTrend = monthStarts
            .Select(monthStart =>
            {
                var point = monthlyCaseCounts.FirstOrDefault(entry =>
                    entry.Year == monthStart.Year && entry.Month == monthStart.Month);

                return new MonthlyTrendPointDto(
                    monthStart.ToDateTime(TimeOnly.MinValue).ToString("MMM"),
                    point?.Count ?? 0);
            })
            .ToArray();

        var outcomes = await dbContext.ResidentCases
            .Include(rc => rc.CaseCategory)
            .GroupBy(rc => rc.CaseCategory != null ? rc.CaseCategory.Name : "Uncategorized")
            .Select(group => new OutcomeDistributionDto(group.Key, group.Count()))
            .OrderByDescending(item => item.Count)
            .ToArrayAsync(ct);

        return new ImpactSummaryDto(
            GeneratedAt: utcNow,
            Metrics: metrics,
            MonthlyTrend: monthlyTrend,
            Outcomes: outcomes);
    }

    private static decimal CalculatePercentChange(int currentValue, int previousValue)
    {
        if (previousValue <= 0)
        {
            return currentValue <= 0 ? 0m : 100m;
        }

        var delta = currentValue - previousValue;
        return Math.Round(delta / (decimal)previousValue * 100m, 1, MidpointRounding.AwayFromZero);
    }
}
