using Microsoft.EntityFrameworkCore;
using Npgsql;
using SafeHarbor.Data;
using SafeHarbor.DTOs;

namespace SafeHarbor.Services.Public;

public sealed class ImpactAggregateService(SafeHarborDbContext dbContext) : IImpactAggregateService
{
    private static readonly string[] LegacyResidentsRelations = ["lighthouse.residents", "public.residents"];
    private static readonly string[] LegacySafehousesRelations = ["lighthouse.safehouses", "public.safehouses"];
    private static readonly string[] LegacyHomeVisitRelations = ["lighthouse.home_visitations", "public.home_visitations"];
    private static readonly string[] LegacyIncidentRelations = ["lighthouse.incident_reports", "public.incident_reports"];
    private static readonly string[] LegacyMonthlyMetricsRelations = ["lighthouse.safehouse_monthly_metrics", "public.safehouse_monthly_metrics"];
    private static readonly HashSet<string> ReintegrationSuccessStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "success",
        "successful",
        "completed",
        "stable",
        "reintegrated"
    };

    public async Task<ImpactSummaryDto> GetAggregateAsync(CancellationToken ct)
    {
        var utcNow = DateTimeOffset.UtcNow;
        var currentMonth = new DateOnly(utcNow.Year, utcNow.Month, 1);
        var previousMonth = currentMonth.AddMonths(-1);
        var nextMonth = currentMonth.AddMonths(1);

        var residentsRelation = await ResolveRelationAsync(LegacyResidentsRelations, ct);
        var safehousesRelation = await ResolveRelationAsync(LegacySafehousesRelations, ct);
        var homeVisitRelation = await ResolveRelationAsync(LegacyHomeVisitRelations, ct);
        var incidentRelation = await ResolveRelationAsync(LegacyIncidentRelations, ct);
        var monthlyMetricsRelation = await ResolveRelationAsync(LegacyMonthlyMetricsRelations, ct);

        var totalResidents = residentsRelation is not null
            ? await ExecuteScalarIntAsync($"SELECT COUNT(*)::int FROM {residentsRelation}", ct)
            : await dbContext.Residents.CountAsync(ct);

        var activeResidents = residentsRelation is not null
            ? await ExecuteScalarIntAsync(
                $"SELECT COUNT(*)::int FROM {residentsRelation} WHERE lower(COALESCE(case_status, '')) = 'active'",
                ct)
            : await dbContext.ResidentCases.CountAsync(rc => rc.ClosedAt == null, ct);

        var reintegratedResidents = residentsRelation is not null
            ? await ExecuteScalarIntAsync(
                $"""
                SELECT COUNT(*)::int
                FROM {residentsRelation}
                WHERE lower(COALESCE(reintegration_status, '')) = ANY(@success_statuses)
                """,
                ct,
                new Dictionary<string, object>
                {
                    ["success_statuses"] = ReintegrationSuccessStatuses.Select(x => x.ToLowerInvariant()).ToArray()
                })
            : await dbContext.ResidentCases.CountAsync(rc => rc.ClosedAt != null, ct);

        var totalSafehouses = safehousesRelation is not null
            ? await ExecuteScalarIntAsync($"SELECT COUNT(*)::int FROM {safehousesRelation}", ct)
            : await dbContext.Safehouses.CountAsync(ct);

        var homeVisitCurrentMonth = homeVisitRelation is not null
            ? await ExecuteScalarIntAsync(
                $"""
                SELECT COUNT(*)::int
                FROM {homeVisitRelation}
                WHERE visit_date >= @currentMonthStart::date AND visit_date < @nextMonthStart::date
                """,
                ct,
                new Dictionary<string, object>
                {
                    ["currentMonthStart"] = currentMonth.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    ["nextMonthStart"] = nextMonth.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                })
            : await dbContext.HomeVisits.CountAsync(hv =>
                hv.VisitDate >= currentMonth.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                && hv.VisitDate < nextMonth.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), ct);

        var homeVisitPreviousMonth = homeVisitRelation is not null
            ? await ExecuteScalarIntAsync(
                $"""
                SELECT COUNT(*)::int
                FROM {homeVisitRelation}
                WHERE visit_date >= @previousMonthStart::date AND visit_date < @currentMonthStart::date
                """,
                ct,
                new Dictionary<string, object>
                {
                    ["previousMonthStart"] = previousMonth.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    ["currentMonthStart"] = currentMonth.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                })
            : await dbContext.HomeVisits.CountAsync(hv =>
                hv.VisitDate >= previousMonth.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                && hv.VisitDate < currentMonth.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), ct);

        var incidentsCurrentMonth = incidentRelation is not null
            ? await ExecuteScalarIntAsync(
                $"""
                SELECT COUNT(*)::int
                FROM {incidentRelation}
                WHERE incident_date >= @currentMonthStart::date AND incident_date < @nextMonthStart::date
                """,
                ct,
                new Dictionary<string, object>
                {
                    ["currentMonthStart"] = currentMonth.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    ["nextMonthStart"] = nextMonth.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                })
            : 0;

        var activeResidentsCurrentMonth = monthlyMetricsRelation is not null
            ? await ExecuteScalarIntAsync(
                $"""
                SELECT COALESCE(SUM(active_residents), 0)::int
                FROM {monthlyMetricsRelation}
                WHERE month_start = @currentMonthStart::date
                """,
                ct,
                new Dictionary<string, object>
                {
                    ["currentMonthStart"] = currentMonth.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                })
            : activeResidents;

        var activeResidentsPreviousMonth = monthlyMetricsRelation is not null
            ? await ExecuteScalarIntAsync(
                $"""
                SELECT COALESCE(SUM(active_residents), 0)::int
                FROM {monthlyMetricsRelation}
                WHERE month_start = @previousMonthStart::date
                """,
                ct,
                new Dictionary<string, object>
                {
                    ["previousMonthStart"] = previousMonth.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                })
            : activeResidents;

        // NOTE: Metrics remain aggregate-only and anonymized for public display.
        var metrics = new ImpactMetricDto[]
        {
            new("Total Residents Served", totalResidents, 0m),
            new("Residents Reintegrated", reintegratedResidents, 0m),
            new("Active Residents", activeResidents, CalculatePercentChange(activeResidentsCurrentMonth, activeResidentsPreviousMonth)),
            new("Safehouses", totalSafehouses, 0m),
            new("Home Visitations (This Month)", homeVisitCurrentMonth, CalculatePercentChange(homeVisitCurrentMonth, homeVisitPreviousMonth)),
            new("Incidents Logged (This Month)", incidentsCurrentMonth, 0m),
        };

        MonthlyTrendPointDto[] monthlyTrend;
        if (monthlyMetricsRelation is not null)
        {
            var monthlyRows = await ExecuteReadAsync(
                $"""
                SELECT to_char(month_start, 'Mon') AS month_label,
                       COALESCE(SUM(active_residents), 0)::int AS active_resident_count
                FROM {monthlyMetricsRelation}
                WHERE month_start >= @startDate::date
                GROUP BY month_start
                ORDER BY month_start
                """,
                reader => new MonthlyTrendPointDto(
                    reader.GetString(0),
                    reader.IsDBNull(1) ? 0 : reader.GetInt32(1)),
                ct,
                new Dictionary<string, object>
                {
                    ["startDate"] = currentMonth.AddMonths(-4).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                });

            monthlyTrend = monthlyRows;
        }
        else if (residentsRelation is not null)
        {
            var monthlyRows = await ExecuteReadAsync(
                $"""
                SELECT to_char(date_trunc('month', date_of_admission), 'Mon') AS month_label,
                       COUNT(*)::int AS admitted_count
                FROM {residentsRelation}
                WHERE date_of_admission >= @startDate::date
                  AND date_of_admission IS NOT NULL
                GROUP BY date_trunc('month', date_of_admission)
                ORDER BY date_trunc('month', date_of_admission)
                """,
                reader => new MonthlyTrendPointDto(
                    reader.GetString(0),
                    reader.IsDBNull(1) ? 0 : reader.GetInt32(1)),
                ct,
                new Dictionary<string, object>
                {
                    ["startDate"] = currentMonth.AddMonths(-4).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                });

            monthlyTrend = monthlyRows;
        }
        else
        {
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

            monthlyTrend = monthStarts
                .Select(monthStart =>
                {
                    var point = monthlyCaseCounts.FirstOrDefault(entry =>
                        entry.Year == monthStart.Year && entry.Month == monthStart.Month);

                    return new MonthlyTrendPointDto(
                        monthStart.ToDateTime(TimeOnly.MinValue).ToString("MMM"),
                        point?.Count ?? 0);
                })
                .ToArray();
        }

        OutcomeDistributionDto[] outcomes;
        if (residentsRelation is not null)
        {
            outcomes = await ExecuteReadAsync(
                $"""
                SELECT
                    CASE
                        WHEN COALESCE(NULLIF(trim(reintegration_status), ''), '') = '' THEN 'In Care / Ongoing Cases'
                        ELSE reintegration_status
                    END AS category,
                    COUNT(*)::int AS resident_count
                FROM {residentsRelation}
                GROUP BY 1
                ORDER BY resident_count DESC
                LIMIT 6
                """,
                reader => new OutcomeDistributionDto(
                    reader.GetString(0),
                    reader.IsDBNull(1) ? 0 : reader.GetInt32(1)),
                ct);
        }
        else
        {
            outcomes = await dbContext.ResidentCases
                .Include(rc => rc.CaseCategory)
                .GroupBy(rc => rc.CaseCategory != null ? rc.CaseCategory.Name : "Uncategorized")
                .Select(group => new OutcomeDistributionDto(group.Key, group.Count()))
                .OrderByDescending(item => item.Count)
                .ToArrayAsync(ct);
        }

        return new ImpactSummaryDto(
            GeneratedAt: utcNow,
            Metrics: metrics,
            MonthlyTrend: monthlyTrend,
            Outcomes: outcomes);
    }

    private async Task<string?> ResolveRelationAsync(string[] candidates, CancellationToken ct)
    {
        foreach (var relation in candidates)
        {
            var parts = relation.Split('.', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                continue;
            }

            if (await RelationExistsAsync(parts[0], parts[1], ct))
            {
                return relation;
            }
        }

        return null;
    }

    private async Task<bool> RelationExistsAsync(string schema, string table, CancellationToken ct)
    {
        var connString = dbContext.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connString))
        {
            return false;
        }

        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = @schema AND table_name = @table)",
            conn);
        cmd.Parameters.AddWithValue("schema", schema);
        cmd.Parameters.AddWithValue("table", table);
        var scalar = await cmd.ExecuteScalarAsync(ct);
        return scalar is bool exists && exists;
    }

    private async Task<int> ExecuteScalarIntAsync(string sql, CancellationToken ct, IReadOnlyDictionary<string, object>? parameters = null)
    {
        var connString = dbContext.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connString))
        {
            return 0;
        }

        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        if (parameters is not null)
        {
            foreach (var (name, value) in parameters)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
        }

        var scalar = await cmd.ExecuteScalarAsync(ct);
        return scalar switch
        {
            int intValue => intValue,
            long longValue => (int)longValue,
            decimal decimalValue => (int)decimalValue,
            _ => 0
        };
    }

    private async Task<T[]> ExecuteReadAsync<T>(
        string sql,
        Func<NpgsqlDataReader, T> map,
        CancellationToken ct,
        IReadOnlyDictionary<string, object>? parameters = null)
    {
        var connString = dbContext.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connString))
        {
            return [];
        }

        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        if (parameters is not null)
        {
            foreach (var (name, value) in parameters)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
        }

        var rows = new List<T>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            rows.Add(map(reader));
        }

        return [.. rows];
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
