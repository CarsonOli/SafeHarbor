using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SafeHarbor.Data;
using SafeHarbor.DTOs;

namespace SafeHarbor.Services.Admin;

public interface IAdminDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken ct);
}

public sealed class AdminDashboardService(SafeHarborDbContext db) : IAdminDashboardService
{
    private const int DashboardListSize = 5;
    private const int SummaryMonths = 3;
    private static readonly string[] LegacyResidentsRelations = ["lighthouse.residents", "public.residents"];
    private static readonly string[] LegacyDonationsRelations = ["lighthouse.donations", "public.donations"];
    private static readonly string[] LegacySupportersRelations = ["lighthouse.supporters", "public.supporters"];
    private static readonly string[] LegacyInterventionRelations = ["lighthouse.intervention_plans", "public.intervention_plans"];
    private static readonly string[] LegacyMetricsRelations = ["lighthouse.safehouse_monthly_metrics", "public.safehouse_monthly_metrics"];

    public async Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken ct)
    {
        var activeResidents = await GetActiveResidentsAsync(ct);
        var recentContributions = await GetRecentContributionsAsync(ct);
        var upcomingConferences = await GetUpcomingConferencesAsync(ct);
        var summaryOutcomes = await GetSummaryOutcomesAsync(ct);

        return new DashboardSummaryResponse(
            activeResidents,
            recentContributions,
            upcomingConferences,
            summaryOutcomes);
    }

    private async Task<int> GetActiveResidentsAsync(CancellationToken ct)
    {
        try
        {
            return await db.ResidentCases.AsNoTracking().CountAsync(x => x.ClosedAt == null, ct);
        }
        catch (PostgresException ex) when (IsMissingRelationOrColumn(ex))
        {
            // NOTE: Older Lighthouse datasets store active/closed state directly on residents rows.
            return await LoadLegacyActiveResidentsAsync(ct);
        }
    }

    private async Task<IReadOnlyCollection<ContributionListItem>> GetRecentContributionsAsync(CancellationToken ct)
    {
        try
        {
            var contributions = await db.Contributions
                .AsNoTracking()
                .OrderByDescending(x => x.ContributionDate)
                .Take(DashboardListSize)
                .Select(x => new ContributionListItem(
                    x.Id,
                    x.Supporter != null && !string.IsNullOrWhiteSpace(x.Supporter.DisplayName) ? x.Supporter.DisplayName : "Unknown supporter",
                    x.Amount,
                    x.ContributionDate,
                    x.StatusState != null && !string.IsNullOrWhiteSpace(x.StatusState.Name) ? x.StatusState.Name : "Recorded"))
                .ToArrayAsync(ct);

            return contributions;
        }
        catch (PostgresException ex) when (IsMissingRelationOrColumn(ex))
        {
            // NOTE: When canonical contribution tables are absent, we read legacy donations instead.
            return await LoadLegacyRecentContributionsAsync(ct);
        }
    }

    private async Task<IReadOnlyCollection<ConferenceListItem>> GetUpcomingConferencesAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        try
        {
            var conferences = await db.CaseConferences
                .AsNoTracking()
                .Where(x => x.ConferenceDate >= now)
                .OrderBy(x => x.ConferenceDate)
                .Take(DashboardListSize)
                .Select(x => new ConferenceListItem(
                    x.Id,
                    x.ResidentCaseId,
                    x.ConferenceDate,
                    x.StatusState != null && !string.IsNullOrWhiteSpace(x.StatusState.Name) ? x.StatusState.Name : "Scheduled",
                    x.OutcomeSummary))
                .ToArrayAsync(ct);

            return conferences;
        }
        catch (PostgresException ex) when (IsMissingRelationOrColumn(ex))
        {
            // NOTE: Legacy datasets schedule conferences via intervention_plans.case_conference_date.
            return await LoadLegacyUpcomingConferencesAsync(now, ct);
        }
    }

    private async Task<IReadOnlyCollection<OutcomeSummaryItem>> GetSummaryOutcomesAsync(CancellationToken ct)
    {
        var legacy = await LoadLegacySummaryOutcomesAsync(ct);
        if (legacy.Count > 0)
        {
            return legacy;
        }

        try
        {
            return await LoadCanonicalSummaryOutcomesAsync(ct);
        }
        catch (PostgresException ex) when (IsMissingRelationOrColumn(ex))
        {
            return [];
        }
    }

    private async Task<IReadOnlyCollection<OutcomeSummaryItem>> LoadCanonicalSummaryOutcomesAsync(CancellationToken ct)
    {
        var monthStartUtc = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var firstTrackedMonth = monthStartUtc.AddMonths(-(SummaryMonths - 1));

        var contributions = await db.Contributions.AsNoTracking()
            .Where(x => x.ContributionDate >= firstTrackedMonth)
            .Select(x => new { x.ContributionDate, x.Amount })
            .ToArrayAsync(ct);

        var visits = await db.HomeVisits.AsNoTracking()
            .Where(x => x.VisitDate >= firstTrackedMonth)
            .Select(x => x.VisitDate)
            .ToArrayAsync(ct);

        var cases = await db.ResidentCases.AsNoTracking()
            .Where(x => x.OpenedAt <= monthStartUtc.AddMonths(1) && (x.ClosedAt == null || x.ClosedAt >= firstTrackedMonth))
            .Select(x => new { x.OpenedAt, x.ClosedAt })
            .ToArrayAsync(ct);

        var outcomes = new List<OutcomeSummaryItem>(SummaryMonths);
        for (var i = 0; i < SummaryMonths; i++)
        {
            var start = firstTrackedMonth.AddMonths(i);
            var end = start.AddMonths(1);

            var totalContributions = contributions
                .Where(x => x.ContributionDate >= start && x.ContributionDate < end)
                .Sum(x => x.Amount);

            var totalHomeVisits = visits.Count(x => x >= start && x < end);

            // Residents served per month are approximated as active caseload at month close,
            // which keeps the dashboard stable even when point-in-time snapshot rows are unavailable.
            var residentsServed = cases.Count(x => x.OpenedAt < end && (x.ClosedAt == null || x.ClosedAt >= start));

            outcomes.Add(new OutcomeSummaryItem(
                DateOnly.FromDateTime(start.UtcDateTime.Date),
                residentsServed,
                totalHomeVisits,
                totalContributions));
        }

        outcomes.Reverse();
        return outcomes;
    }

    private async Task<int> LoadLegacyActiveResidentsAsync(CancellationToken ct)
    {
        var residentsRelation = await ResolveRelationAsync(LegacyResidentsRelations, ct);
        if (residentsRelation is null)
        {
            return 0;
        }

        try
        {
            const string sql = """
                SELECT COUNT(*)::int
                FROM {0}
                WHERE date_closed IS NULL
                  AND COALESCE(NULLIF(TRIM(case_status), ''), 'Active') !~* 'closed|inactive|archived'
                """;
            return await ExecuteScalarIntAsync(string.Format(sql, residentsRelation), ct);
        }
        catch (PostgresException ex) when (IsMissingRelationOrColumn(ex))
        {
            return 0;
        }
    }

    private async Task<IReadOnlyCollection<ContributionListItem>> LoadLegacyRecentContributionsAsync(CancellationToken ct)
    {
        var donationsRelation = await ResolveRelationAsync(LegacyDonationsRelations, ct);
        if (donationsRelation is null)
        {
            return [];
        }

        var supportersRelation = await ResolveRelationAsync(LegacySupportersRelations, ct);
        var sql = supportersRelation is null
            ? $"""
                SELECT
                    d.donation_id,
                    'Unknown supporter' AS donor_name,
                    COALESCE(d.amount, 0) AS amount,
                    COALESCE(d.donation_date, CURRENT_TIMESTAMP) AS donation_date,
                    COALESCE(NULLIF(TRIM(d.status), ''), 'Recorded') AS status
                FROM {donationsRelation} d
                ORDER BY d.donation_date DESC NULLS LAST, d.donation_id DESC
                LIMIT @limit
                """
            : $"""
                SELECT
                    d.donation_id,
                    COALESCE(NULLIF(TRIM(s.display_name), ''), NULLIF(TRIM(s.organization_name), ''), 'Unknown supporter') AS donor_name,
                    COALESCE(d.amount, 0) AS amount,
                    COALESCE(d.donation_date, CURRENT_TIMESTAMP) AS donation_date,
                    COALESCE(NULLIF(TRIM(d.status), ''), 'Recorded') AS status
                FROM {donationsRelation} d
                LEFT JOIN {supportersRelation} s ON s.supporter_id = d.supporter_id
                ORDER BY d.donation_date DESC NULLS LAST, d.donation_id DESC
                LIMIT @limit
                """;

        try
        {
            var rows = await ExecuteReadAsync(
                sql,
                reader => new ContributionListItem(
                    BuildDeterministicGuid("legacy-donation", Convert.ToInt64(reader.GetValue(0), CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? 0m : reader.GetDecimal(2),
                    ReadDateTimeOffset(reader, 3),
                    reader.GetString(4)),
                ct,
                new Dictionary<string, object> { ["limit"] = DashboardListSize });

            return rows;
        }
        catch (PostgresException ex) when (IsMissingRelationOrColumn(ex))
        {
            return [];
        }
    }

    private async Task<IReadOnlyCollection<ConferenceListItem>> LoadLegacyUpcomingConferencesAsync(DateTimeOffset now, CancellationToken ct)
    {
        var interventionRelation = await ResolveRelationAsync(LegacyInterventionRelations, ct);
        if (interventionRelation is null)
        {
            return [];
        }

        var sql = $"""
            SELECT
                plan_id,
                resident_id,
                case_conference_date,
                COALESCE(NULLIF(TRIM(status), ''), 'Scheduled') AS status,
                COALESCE(NULLIF(TRIM(plan_description), ''), 'Conference scheduled from intervention plan')
            FROM {interventionRelation}
            WHERE case_conference_date IS NOT NULL
              AND case_conference_date::timestamp >= @now
            ORDER BY case_conference_date ASC, plan_id ASC
            LIMIT @limit
            """;

        try
        {
            return await ExecuteReadAsync(
                sql,
                reader =>
                {
                    var planId = Convert.ToInt64(reader.GetValue(0), CultureInfo.InvariantCulture);
                    var residentId = reader.IsDBNull(1) ? 0L : Convert.ToInt64(reader.GetValue(1), CultureInfo.InvariantCulture);
                    var conferenceDate = reader.GetDateTime(2);
                    return new ConferenceListItem(
                        BuildDeterministicGuid("legacy-conference", planId.ToString(CultureInfo.InvariantCulture)),
                        BuildDeterministicGuid("resident-case", residentId.ToString(CultureInfo.InvariantCulture)),
                        new DateTimeOffset(DateTime.SpecifyKind(conferenceDate, DateTimeKind.Utc)),
                        reader.GetString(3),
                        reader.GetString(4));
                },
                ct,
                new Dictionary<string, object>
                {
                    ["now"] = now.UtcDateTime,
                    ["limit"] = DashboardListSize
                });
        }
        catch (PostgresException ex) when (IsMissingRelationOrColumn(ex))
        {
            return [];
        }
    }

    private async Task<IReadOnlyCollection<OutcomeSummaryItem>> LoadLegacySummaryOutcomesAsync(CancellationToken ct)
    {
        var metricsRelation = await ResolveRelationAsync(LegacyMetricsRelations, ct);
        if (metricsRelation is null)
        {
            return [];
        }

        var donationRelation = await ResolveRelationAsync(LegacyDonationsRelations, ct);
        var monthlyDonations = donationRelation is null
            ? new Dictionary<DateOnly, decimal>()
            : (await ExecuteReadAsync(
                $"""
                SELECT
                    date_trunc('month', donation_date)::date AS month_start,
                    COALESCE(SUM(COALESCE(amount, 0)), 0) AS total_amount
                FROM {donationRelation}
                WHERE donation_date IS NOT NULL
                GROUP BY 1
                """,
                reader => new
                {
                    Month = DateOnly.FromDateTime(reader.GetDateTime(0).Date),
                    Amount = reader.IsDBNull(1) ? 0m : reader.GetDecimal(1)
                },
                ct))
                .ToDictionary(x => x.Month, x => x.Amount);

        try
        {
            var rows = await ExecuteReadAsync(
                $"""
                SELECT
                    month_start,
                    COALESCE(SUM(active_residents), 0) AS total_residents,
                    COALESCE(SUM(home_visitation_count), 0) AS total_visits
                FROM {metricsRelation}
                WHERE month_start IS NOT NULL
                GROUP BY month_start
                ORDER BY month_start DESC
                LIMIT @limit
                """,
                reader =>
                {
                    var snapshotDate = DateOnly.FromDateTime(reader.GetDateTime(0).Date);
                    return new OutcomeSummaryItem(
                        snapshotDate,
                        reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                        reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                        monthlyDonations.TryGetValue(snapshotDate, out var amount) ? amount : 0m);
                },
                ct,
                new Dictionary<string, object> { ["limit"] = SummaryMonths });

            return rows;
        }
        catch (PostgresException ex) when (IsMissingRelationOrColumn(ex))
        {
            return [];
        }
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
        var connString = db.Database.GetConnectionString();
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

    private async Task<int> ExecuteScalarIntAsync(
        string sql,
        CancellationToken ct,
        IReadOnlyDictionary<string, object>? parameters = null)
    {
        var connString = db.Database.GetConnectionString();
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
        return scalar is null || scalar is DBNull ? 0 : Convert.ToInt32(scalar, CultureInfo.InvariantCulture);
    }

    private async Task<T[]> ExecuteReadAsync<T>(
        string sql,
        Func<NpgsqlDataReader, T> map,
        CancellationToken ct,
        IReadOnlyDictionary<string, object>? parameters = null)
    {
        var connString = db.Database.GetConnectionString();
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

        var results = new List<T>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(map(reader));
        }

        return [.. results];
    }

    private static Guid BuildDeterministicGuid(string scope, string key)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes($"{scope}:{key}"));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }

    private static bool IsMissingRelationOrColumn(PostgresException ex) =>
        ex.SqlState == PostgresErrorCodes.UndefinedTable || ex.SqlState == PostgresErrorCodes.UndefinedColumn;

    private static DateTimeOffset ReadDateTimeOffset(NpgsqlDataReader reader, int ordinal)
    {
        var value = reader.GetValue(ordinal);
        return value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
            DateOnly date => new DateTimeOffset(DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)),
            _ => DateTimeOffset.UtcNow
        };
    }
}
