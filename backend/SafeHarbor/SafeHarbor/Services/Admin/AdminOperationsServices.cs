using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SafeHarbor.Data;
using SafeHarbor.DTOs;
using SafeHarbor.Models.Entities;
using SafeHarbor.Models.Enums;

namespace SafeHarbor.Services.Admin;

public interface ICaseloadInventoryService
{
    Task<PagedResult<ResidentCaseListItem>> GetResidentsAsync(PagingQuery query, CancellationToken ct);
    Task<CaseloadLookupsResponse> GetLookupsAsync(CancellationToken ct);
    Task<ResidentCaseListItem> CreateResidentCaseAsync(CreateResidentCaseRequest request, CancellationToken ct);
    Task<ResidentCaseListItem?> UpdateResidentCaseAsync(Guid id, UpdateResidentCaseRequest request, CancellationToken ct);
    Task<bool> DeleteResidentCaseAsync(Guid id, CancellationToken ct);
}

public interface IProcessRecordingService
{
    Task<PagedResult<ProcessRecordItem>> GetAsync(PagingQuery query, CancellationToken ct);
    Task<ProcessRecordItem> CreateAsync(CreateProcessRecordRequest request, CancellationToken ct);
    Task<ProcessRecordItem?> UpdateAsync(Guid id, CreateProcessRecordRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

public interface IVisitationConferenceService
{
    Task<PagedResult<HomeVisitItem>> GetVisitsAsync(PagingQuery query, CancellationToken ct);
    Task<PagedResult<CaseConferenceItem>> GetUpcomingAsync(PagingQuery query, CancellationToken ct);
    Task<PagedResult<CaseConferenceItem>> GetPreviousAsync(PagingQuery query, CancellationToken ct);
    Task<HomeVisitItem> CreateVisitAsync(CreateHomeVisitRequest request, CancellationToken ct);
    Task<CaseConferenceItem> CreateConferenceAsync(CreateCaseConferenceRequest request, CancellationToken ct);
}

public interface IDonorContributionService
{
    Task<PagedResult<DonorListItem>> GetDonorsAsync(PagingQuery query, CancellationToken ct);
    Task<DonorListItem> CreateDonorAsync(CreateDonorRequest request, CancellationToken ct);
    Task<ContributionListItem> CreateContributionAsync(CreateContributionRequest request, CancellationToken ct);
    Task<bool> CreateAllocationAsync(CreateAllocationRequest request, CancellationToken ct);
}

public interface IReportsAnalyticsService
{
    Task<ReportsAnalyticsResponse> GetAsync(CancellationToken ct);
}

public sealed class CaseloadInventoryService(SafeHarborDbContext db) : ICaseloadInventoryService
{
    public async Task<PagedResult<ResidentCaseListItem>> GetResidentsAsync(PagingQuery query, CancellationToken ct)
    {
        var page = query.NormalizedPage;
        var pageSize = query.NormalizedPageSize;
        if (!await HasCanonicalCaseloadSchemaAsync(ct))
        {
            return await GetLegacyResidentsAsync(query, ct);
        }

        try
        {
            var q = db.ResidentCases.AsNoTracking()
                .Include(x => x.Safehouse)
                .Include(x => x.CaseCategory)
                .Include(x => x.StatusState)
                .Include(x => x.Resident)
                .AsQueryable();

            if (query.SafehouseId is { } safehouseId)
            {
                q = q.Where(x => x.SafehouseId == safehouseId);
            }

            if (query.StatusStateId is { } statusStateId)
            {
                q = q.Where(x => x.StatusStateId == statusStateId);
            }

            if (query.CategoryId is { } categoryId)
            {
                q = q.Where(x => x.CaseCategoryId == categoryId);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim().ToLower();
                q = q.Where(x =>
                    (x.Safehouse != null && x.Safehouse.Name.ToLower().Contains(search)) ||
                    (x.CaseCategory != null && x.CaseCategory.Name.ToLower().Contains(search)) ||
                    (x.StatusState != null && x.StatusState.Name.ToLower().Contains(search)) ||
                    (x.Resident != null && x.Resident.FullName.ToLower().Contains(search)));
            }

            q = query.Desc ? q.OrderByDescending(x => x.OpenedAt) : q.OrderBy(x => x.OpenedAt);

            var total = await q.CountAsync(ct);
            var items = await q.Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ResidentCaseListItem(
                    x.Id,
                    x.SafehouseId,
                    x.Safehouse != null ? x.Safehouse.Name : "Unknown",
                    x.CaseCategoryId,
                    x.CaseCategory != null ? x.CaseCategory.Name : "Unknown",
                    x.StatusStateId,
                    x.StatusState != null ? x.StatusState.Name : "Unknown",
                    x.CreatedBy,
                    x.Resident != null ? x.Resident.FullName : null,
                    x.OpenedAt,
                    x.ClosedAt,
                    x.ResidentId))
                .ToArrayAsync(ct);

            return new PagedResult<ResidentCaseListItem>(items, page, pageSize, total);
        }
        catch (PostgresException ex) when (IsMissingCaseloadSchema(ex))
        {
            // NOTE: Some production databases still run the pre-canonical schema (no resident_cases table).
            // In that shape, we derive "case" rows from lighthouse.residents to keep caseload usable.
            return await GetLegacyResidentsAsync(query, ct);
        }
    }

    public async Task<CaseloadLookupsResponse> GetLookupsAsync(CancellationToken ct)
    {
        if (!await HasCanonicalCaseloadSchemaAsync(ct))
        {
            return await GetLegacyLookupsAsync(ct);
        }

        try
        {
            var safehouses = await db.Safehouses.AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new CaseloadSafehouseItem(x.Id.ToString(), x.Name))
                .ToArrayAsync(ct);

            var categories = await db.CaseCategories.AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new CaseloadLookupItem(x.Id, x.Name))
                .ToArrayAsync(ct);

            var statuses = await db.StatusState.AsNoTracking()
                .Where(x => x.Domain == StatusDomain.ResidentCase)
                .OrderBy(x => x.Name)
                .Select(x => new CaseloadLookupItem(x.Id, x.Name))
                .ToArrayAsync(ct);

            return new CaseloadLookupsResponse(safehouses, categories, statuses);
        }
        catch (PostgresException ex) when (IsMissingCaseloadSchema(ex))
        {
            // NOTE: Legacy datasets store status/category directly on residents records.
            // Build lookup options from those columns so filters still work before schema migration.
            return await GetLegacyLookupsAsync(ct);
        }
    }

    public async Task<ResidentCaseListItem> CreateResidentCaseAsync(CreateResidentCaseRequest request, CancellationToken ct)
    {
        var entity = new ResidentCase
        {
            Id = Guid.NewGuid(),
            SafehouseId = request.SafehouseId,
            CaseCategoryId = request.CaseCategoryId,
            CaseSubcategoryId = request.CaseSubcategoryId,
            StatusStateId = request.StatusStateId,
            ResidentId = request.ResidentUserId,
            OpenedAt = request.OpenedAt ?? DateTimeOffset.UtcNow
        };

        db.ResidentCases.Add(entity);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<ResidentCaseListItem?> UpdateResidentCaseAsync(Guid id, UpdateResidentCaseRequest request, CancellationToken ct)
    {
        var entity = await db.ResidentCases.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return null;

        entity.SafehouseId = request.SafehouseId;
        entity.CaseCategoryId = request.CaseCategoryId;
        entity.CaseSubcategoryId = request.CaseSubcategoryId;
        entity.StatusStateId = request.StatusStateId;
        entity.ResidentId = request.ResidentUserId;
        entity.ClosedAt = request.ClosedAt;

        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<bool> DeleteResidentCaseAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.ResidentCases.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return false;

        db.ResidentCases.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task<ResidentCaseListItem> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await db.ResidentCases.AsNoTracking()
            .Include(x => x.Safehouse)
            .Include(x => x.CaseCategory)
            .Include(x => x.StatusState)
            .Where(x => x.Id == id)
            .Select(x => new ResidentCaseListItem(
                x.Id,
                x.SafehouseId,
                x.Safehouse != null ? x.Safehouse.Name : "Unknown",
                x.CaseCategoryId,
                x.CaseCategory != null ? x.CaseCategory.Name : "Unknown",
                x.StatusStateId,
                x.StatusState != null ? x.StatusState.Name : "Unknown",
                x.CreatedBy,
                x.Resident != null ? x.Resident.FullName : null,
                x.OpenedAt,
                x.ClosedAt,
                x.ResidentId))
            .FirstAsync(ct);
    }

    private static bool IsMissingCaseloadSchema(PostgresException ex) =>
        ex.SqlState == PostgresErrorCodes.UndefinedTable || ex.SqlState == PostgresErrorCodes.UndefinedColumn;

    private async Task<bool> HasCanonicalCaseloadSchemaAsync(CancellationToken ct)
    {
        return await RelationHasColumnsAsync("lighthouse", "resident_cases", ["id", "safehouse_id", "resident_id", "case_category_id", "status_state_id", "opened_at"], ct)
            && await RelationHasColumnsAsync("lighthouse", "safehouses", ["id", "name"], ct)
            && await RelationHasColumnsAsync("lighthouse", "case_category", ["id", "name"], ct)
            && await RelationHasColumnsAsync("lighthouse", "status_state", ["id", "name", "domain"], ct);
    }

    private async Task<CaseloadLookupsResponse> GetLegacyLookupsAsync(CancellationToken ct)
    {
        var safehouseRows = await ExecuteReadAsync(
            """
            SELECT safehouse_id, COALESCE(NULLIF(TRIM(name), ''), CONCAT('Safehouse #', safehouse_id::text))
            FROM lighthouse.safehouses
            ORDER BY 2
            """,
            reader => new
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            },
            ct);

        var categories = await ExecuteReadAsync(
            """
            SELECT DISTINCT COALESCE(NULLIF(TRIM(case_category), ''), 'Uncategorized') AS value
            FROM lighthouse.residents
            ORDER BY 1
            """,
            reader => reader.GetString(0),
            ct);

        var statuses = await ExecuteReadAsync(
            """
            SELECT DISTINCT COALESCE(NULLIF(TRIM(case_status), ''), 'Unknown') AS value
            FROM lighthouse.residents
            ORDER BY 1
            """,
            reader => reader.GetString(0),
            ct);

        var safehouses = safehouseRows
            .Select(x => new CaseloadSafehouseItem(
                BuildDeterministicGuid("safehouse", x.Id.ToString(CultureInfo.InvariantCulture)).ToString(),
                x.Name))
            .ToArray();
        var categoryLookups = BuildLegacyLookupItems(categories, "case-category");
        var statusLookups = BuildLegacyLookupItems(statuses, "status");

        return new CaseloadLookupsResponse(safehouses, categoryLookups, statusLookups);
    }

    private async Task<PagedResult<ResidentCaseListItem>> GetLegacyResidentsAsync(PagingQuery query, CancellationToken ct)
    {
        var categoryLookupById = BuildLegacyLookupMap(
            await ExecuteReadAsync(
                """
                SELECT DISTINCT COALESCE(NULLIF(TRIM(case_category), ''), 'Uncategorized') AS value
                FROM lighthouse.residents
                ORDER BY 1
                """,
                reader => reader.GetString(0),
                ct),
            "case-category");

        var statusLookupById = BuildLegacyLookupMap(
            await ExecuteReadAsync(
                """
                SELECT DISTINCT COALESCE(NULLIF(TRIM(case_status), ''), 'Unknown') AS value
                FROM lighthouse.residents
                ORDER BY 1
                """,
                reader => reader.GetString(0),
                ct),
            "status");

        if (query.StatusStateId is { } statusFilterId && !statusLookupById.TryGetValue(statusFilterId, out _))
        {
            return new PagedResult<ResidentCaseListItem>([], query.NormalizedPage, query.NormalizedPageSize, 0);
        }

        if (query.CategoryId is { } categoryFilterId && !categoryLookupById.TryGetValue(categoryFilterId, out _))
        {
            return new PagedResult<ResidentCaseListItem>([], query.NormalizedPage, query.NormalizedPageSize, 0);
        }

        var where = new List<string>();
        var parameters = new Dictionary<string, object>();

        if (query.SafehouseId is { } safehouseFilter)
        {
            var safehouseLookupRows = await ExecuteReadAsync(
                """
                SELECT safehouse_id
                FROM lighthouse.safehouses
                """,
                reader => reader.GetInt32(0),
                ct);

            int? safehouseId = safehouseLookupRows
                .Select(id => (int?)id)
                .FirstOrDefault(id => id.HasValue && BuildDeterministicGuid("safehouse", id.Value.ToString(CultureInfo.InvariantCulture)) == safehouseFilter);
            if (safehouseId is null)
            {
                return new PagedResult<ResidentCaseListItem>([], query.NormalizedPage, query.NormalizedPageSize, 0);
            }

            where.Add("r.safehouse_id = @safehouse_id");
            parameters["safehouse_id"] = safehouseId.Value;
        }

        if (query.StatusStateId is { } statusId && statusLookupById.TryGetValue(statusId, out var statusName))
        {
            where.Add("COALESCE(NULLIF(TRIM(r.case_status), ''), 'Unknown') = @status");
            parameters["status"] = statusName;
        }

        if (query.CategoryId is { } categoryId && categoryLookupById.TryGetValue(categoryId, out var categoryName))
        {
            where.Add("COALESCE(NULLIF(TRIM(r.case_category), ''), 'Uncategorized') = @category");
            parameters["category"] = categoryName;
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            where.Add(
                """
                (
                    COALESCE(NULLIF(TRIM(r.case_control_no), ''), NULLIF(TRIM(r.internal_code), ''), CONCAT('Resident #', r.resident_id::text)) ILIKE @search
                    OR COALESCE(NULLIF(TRIM(s.name), ''), CONCAT('Safehouse #', COALESCE(r.safehouse_id, 0)::text)) ILIKE @search
                    OR COALESCE(NULLIF(TRIM(r.case_category), ''), 'Uncategorized') ILIKE @search
                    OR COALESCE(NULLIF(TRIM(r.case_status), ''), 'Unknown') ILIKE @search
                )
                """);
            parameters["search"] = $"%{query.Search.Trim()}%";
        }

        var whereClause = where.Count > 0 ? $"WHERE {string.Join(" AND ", where)}" : string.Empty;

        var total = await ExecuteScalarIntAsync(
            $"""
            SELECT COUNT(*)::int
            FROM lighthouse.residents r
            LEFT JOIN lighthouse.safehouses s ON s.safehouse_id = r.safehouse_id
            {whereClause}
            """,
            ct,
            parameters);

        var pagingParameters = new Dictionary<string, object>(parameters)
        {
            ["limit"] = query.NormalizedPageSize,
            ["offset"] = (query.NormalizedPage - 1) * query.NormalizedPageSize
        };
        var orderDirection = query.Desc ? "DESC" : "ASC";

        var rows = await ExecuteReadAsync(
            $"""
            SELECT
                r.resident_id,
                r.safehouse_id,
                COALESCE(NULLIF(TRIM(s.name), ''), CONCAT('Safehouse #', COALESCE(r.safehouse_id, 0)::text)) AS safehouse_name,
                COALESCE(NULLIF(TRIM(r.case_category), ''), 'Uncategorized') AS category_name,
                COALESCE(NULLIF(TRIM(r.case_status), ''), 'Unknown') AS status_name,
                NULLIF(TRIM(r.assigned_social_worker), '') AS social_worker,
                COALESCE(r.date_of_admission::timestamp, r.created_at, CURRENT_TIMESTAMP) AS opened_at,
                r.date_closed::timestamp AS closed_at,
                COALESCE(NULLIF(TRIM(r.case_control_no), ''), NULLIF(TRIM(r.internal_code), ''), CONCAT('Resident #', r.resident_id::text)) AS resident_name
            FROM lighthouse.residents r
            LEFT JOIN lighthouse.safehouses s ON s.safehouse_id = r.safehouse_id
            {whereClause}
            ORDER BY COALESCE(r.date_of_admission::timestamp, r.created_at, CURRENT_TIMESTAMP) {orderDirection}, r.resident_id {orderDirection}
            LIMIT @limit OFFSET @offset
            """,
            reader => new
            {
                ResidentId = reader.GetInt32(0),
                SafehouseId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                SafehouseName = reader.GetString(2),
                CategoryName = reader.GetString(3),
                StatusName = reader.GetString(4),
                SocialWorker = reader.IsDBNull(5) ? null : reader.GetString(5),
                OpenedAt = reader.IsDBNull(6) ? DateTime.UtcNow : reader.GetDateTime(6),
                ClosedAt = reader.IsDBNull(7) ? (DateTime?)null : reader.GetDateTime(7),
                ResidentName = reader.GetString(8)
            },
            ct,
            pagingParameters);

        var items = rows.Select(row =>
        {
            var categoryId = BuildLegacyLookupId("case-category", row.CategoryName);
            var statusId = BuildLegacyLookupId("status", row.StatusName);
            var safehouseGuid = BuildDeterministicGuid("safehouse", row.SafehouseId.ToString(CultureInfo.InvariantCulture));
            var residentGuid = BuildDeterministicGuid("resident", row.ResidentId.ToString(CultureInfo.InvariantCulture));
            var caseGuid = BuildDeterministicGuid("resident-case", row.ResidentId.ToString(CultureInfo.InvariantCulture));
            return new ResidentCaseListItem(
                caseGuid,
                safehouseGuid,
                row.SafehouseName,
                categoryId,
                row.CategoryName,
                statusId,
                row.StatusName,
                row.SocialWorker,
                row.ResidentName,
                new DateTimeOffset(DateTime.SpecifyKind(row.OpenedAt, DateTimeKind.Utc)),
                row.ClosedAt is null ? null : new DateTimeOffset(DateTime.SpecifyKind(row.ClosedAt.Value, DateTimeKind.Utc)),
                residentGuid);
        }).ToArray();

        return new PagedResult<ResidentCaseListItem>(items, query.NormalizedPage, query.NormalizedPageSize, total);
    }

    private static CaseloadLookupItem[] BuildLegacyLookupItems(IEnumerable<string> labels, string namespaceKey) =>
        labels
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(label => new CaseloadLookupItem(BuildLegacyLookupId(namespaceKey, label), label))
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static IReadOnlyDictionary<int, string> BuildLegacyLookupMap(IEnumerable<string> labels, string namespaceKey) =>
        BuildLegacyLookupItems(labels, namespaceKey).ToDictionary(x => x.Id, x => x.Name);

    private static int BuildLegacyLookupId(string namespaceKey, string label)
    {
        var normalized = $"{namespaceKey}:{label.Trim().ToLowerInvariant()}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        var value = BitConverter.ToInt32(hash, 0) & int.MaxValue;
        return value == 0 ? 1 : value;
    }

    private static Guid BuildDeterministicGuid(string namespaceKey, string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{namespaceKey}:{value}"));
        Span<byte> guidBytes = stackalloc byte[16];
        bytes.AsSpan(0, 16).CopyTo(guidBytes);
        return new Guid(guidBytes);
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

        var rows = new List<T>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            rows.Add(map(reader));
        }

        return [.. rows];
    }

    private async Task<bool> RelationHasColumnsAsync(
        string schema,
        string table,
        IReadOnlyCollection<string> requiredColumns,
        CancellationToken ct)
    {
        var connString = db.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connString))
        {
            return false;
        }

        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(
            """
            SELECT COUNT(*)::int
            FROM information_schema.columns
            WHERE table_schema = @schema
              AND table_name = @table
              AND column_name = ANY(@columns)
            """,
            conn);
        cmd.Parameters.AddWithValue("schema", schema);
        cmd.Parameters.AddWithValue("table", table);
        cmd.Parameters.AddWithValue("columns", requiredColumns.ToArray());

        var count = await cmd.ExecuteScalarAsync(ct);
        return count is int intCount && intCount == requiredColumns.Count;
    }
}

public sealed class ProcessRecordingService(SafeHarborDbContext db) : IProcessRecordingService
{
    public async Task<PagedResult<ProcessRecordItem>> GetAsync(PagingQuery query, CancellationToken ct)
    {
        var q = db.ProcessRecordings.AsNoTracking().AsQueryable();

        if (query.ResidentCaseId is { } residentCaseId)
        {
            q = q.Where(x => x.ResidentCaseId == residentCaseId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            q = q.Where(x =>
                x.Summary.ToLower().Contains(search) ||
                x.SocialWorker.ToLower().Contains(search) ||
                x.EmotionalStateObserved.ToLower().Contains(search) ||
                (x.InterventionsApplied != null && x.InterventionsApplied.ToLower().Contains(search)));
        }

        q = query.Desc ? q.OrderByDescending(x => x.RecordedAt) : q.OrderBy(x => x.RecordedAt);

        var total = await q.CountAsync(ct);
        var page = query.NormalizedPage;
        var pageSize = query.NormalizedPageSize;

        var items = await q.Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ProcessRecordItem(
                x.Id,
                x.ResidentCaseId,
                x.RecordedAt,
                x.SocialWorker,
                x.SessionType,
                x.SessionDurationMinutes,
                x.EmotionalStateObserved,
                x.EmotionalStateEnd,
                x.Summary,
                x.InterventionsApplied,
                x.FollowUpActions,
                x.ProgressNoted,
                x.ConcernsFlagged,
                x.ReferralMade,
                x.NotesRestricted != null && x.NotesRestricted != string.Empty))
            .ToArrayAsync(ct);

        return new PagedResult<ProcessRecordItem>(items, page, pageSize, total);
    }

    public async Task<ProcessRecordItem> CreateAsync(CreateProcessRecordRequest request, CancellationToken ct)
    {
        var entity = new ProcessRecording
        {
            Id = Guid.NewGuid(),
            ResidentCaseId = request.ResidentCaseId,
            RecordedAt = request.RecordedAt ?? DateTimeOffset.UtcNow,
            SocialWorker = request.SocialWorker,
            SessionType = request.SessionType,
            SessionDurationMinutes = request.SessionDurationMinutes,
            EmotionalStateObserved = request.EmotionalStateObserved,
            EmotionalStateEnd = request.EmotionalStateEnd,
            Summary = request.Summary,
            InterventionsApplied = request.InterventionsApplied,
            FollowUpActions = request.FollowUpActions,
            ProgressNoted = request.ProgressNoted,
            ConcernsFlagged = request.ConcernsFlagged,
            ReferralMade = request.ReferralMade,
            NotesRestricted = request.NotesRestricted
        };

        db.ProcessRecordings.Add(entity);
        await db.SaveChangesAsync(ct);

        return new ProcessRecordItem(
            entity.Id, entity.ResidentCaseId, entity.RecordedAt,
            entity.SocialWorker, entity.SessionType, entity.SessionDurationMinutes,
            entity.EmotionalStateObserved, entity.EmotionalStateEnd, entity.Summary,
            entity.InterventionsApplied, entity.FollowUpActions,
            entity.ProgressNoted, entity.ConcernsFlagged, entity.ReferralMade,
            entity.NotesRestricted != null && entity.NotesRestricted != string.Empty);
    }

    public async Task<ProcessRecordItem?> UpdateAsync(Guid id, CreateProcessRecordRequest request, CancellationToken ct)
    {
        var entity = await db.ProcessRecordings.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return null;

        entity.ResidentCaseId = request.ResidentCaseId;
        entity.RecordedAt = request.RecordedAt ?? entity.RecordedAt;
        entity.SocialWorker = request.SocialWorker;
        entity.SessionType = request.SessionType;
        entity.SessionDurationMinutes = request.SessionDurationMinutes;
        entity.EmotionalStateObserved = request.EmotionalStateObserved;
        entity.EmotionalStateEnd = request.EmotionalStateEnd;
        entity.Summary = request.Summary;
        entity.InterventionsApplied = request.InterventionsApplied;
        entity.FollowUpActions = request.FollowUpActions;
        entity.ProgressNoted = request.ProgressNoted;
        entity.ConcernsFlagged = request.ConcernsFlagged;
        entity.ReferralMade = request.ReferralMade;
        entity.NotesRestricted = request.NotesRestricted;

        await db.SaveChangesAsync(ct);
        return new ProcessRecordItem(
            entity.Id, entity.ResidentCaseId, entity.RecordedAt,
            entity.SocialWorker, entity.SessionType, entity.SessionDurationMinutes,
            entity.EmotionalStateObserved, entity.EmotionalStateEnd, entity.Summary,
            entity.InterventionsApplied, entity.FollowUpActions,
            entity.ProgressNoted, entity.ConcernsFlagged, entity.ReferralMade,
            entity.NotesRestricted != null && entity.NotesRestricted != string.Empty);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.ProcessRecordings.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return false;

        db.ProcessRecordings.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public sealed class VisitationConferenceService(SafeHarborDbContext db) : IVisitationConferenceService
{
    public async Task<PagedResult<HomeVisitItem>> GetVisitsAsync(PagingQuery query, CancellationToken ct)
    {
        var q = db.HomeVisits.AsNoTracking().Include(x => x.VisitType).Include(x => x.StatusState).AsQueryable();

        if (query.ResidentCaseId is { } residentCaseId)
        {
            q = q.Where(x => x.ResidentCaseId == residentCaseId);
        }

        if (query.StatusStateId is { } statusStateId)
        {
            q = q.Where(x => x.StatusStateId == statusStateId);
        }

        q = query.Desc ? q.OrderByDescending(x => x.VisitDate) : q.OrderBy(x => x.VisitDate);

        var total = await q.CountAsync(ct);
        var page = query.NormalizedPage;
        var pageSize = query.NormalizedPageSize;

        var items = await q.Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new HomeVisitItem(
                x.Id,
                x.ResidentCaseId,
                x.VisitDate,
                x.VisitType != null ? x.VisitType.Name : "Unknown",
                x.StatusState != null ? x.StatusState.Name : "Unknown",
                x.Notes))
            .ToArrayAsync(ct);

        return new PagedResult<HomeVisitItem>(items, page, pageSize, total);
    }

    public Task<PagedResult<CaseConferenceItem>> GetUpcomingAsync(PagingQuery query, CancellationToken ct)
        => GetConferencesAsync(query, ct, upcoming: true);

    public Task<PagedResult<CaseConferenceItem>> GetPreviousAsync(PagingQuery query, CancellationToken ct)
        => GetConferencesAsync(query, ct, upcoming: false);

    private async Task<PagedResult<CaseConferenceItem>> GetConferencesAsync(PagingQuery query, CancellationToken ct, bool upcoming)
    {
        var now = DateTimeOffset.UtcNow;
        var q = db.CaseConferences.AsNoTracking().Include(x => x.StatusState).AsQueryable();

        q = upcoming ? q.Where(x => x.ConferenceDate >= now) : q.Where(x => x.ConferenceDate < now);

        if (query.ResidentCaseId is { } residentCaseId)
        {
            q = q.Where(x => x.ResidentCaseId == residentCaseId);
        }

        if (query.StatusStateId is { } statusStateId)
        {
            q = q.Where(x => x.StatusStateId == statusStateId);
        }

        q = upcoming ? q.OrderBy(x => x.ConferenceDate) : q.OrderByDescending(x => x.ConferenceDate);

        var total = await q.CountAsync(ct);
        var page = query.NormalizedPage;
        var pageSize = query.NormalizedPageSize;

        var items = await q.Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CaseConferenceItem(
                x.Id,
                x.ResidentCaseId,
                x.ConferenceDate,
                x.StatusState != null ? x.StatusState.Name : "Unknown",
                x.OutcomeSummary))
            .ToArrayAsync(ct);

        return new PagedResult<CaseConferenceItem>(items, page, pageSize, total);
    }

    public async Task<HomeVisitItem> CreateVisitAsync(CreateHomeVisitRequest request, CancellationToken ct)
    {
        var entity = new HomeVisit
        {
            Id             = Guid.NewGuid(),
            ResidentCaseId = request.ResidentCaseId,
            VisitTypeId    = request.VisitTypeId,
            StatusStateId  = request.StatusStateId,
            VisitDate      = request.VisitDate,
            Notes          = request.Notes ?? string.Empty,
        };
        db.HomeVisits.Add(entity);
        await db.SaveChangesAsync(ct);
        await db.Entry(entity).Reference(x => x.VisitType).LoadAsync(ct);
        await db.Entry(entity).Reference(x => x.StatusState).LoadAsync(ct);
        return new HomeVisitItem(
            entity.Id, entity.ResidentCaseId, entity.VisitDate,
            entity.VisitType?.Name ?? "Unknown",
            entity.StatusState?.Name ?? "Unknown",
            entity.Notes);
    }

    public async Task<CaseConferenceItem> CreateConferenceAsync(CreateCaseConferenceRequest request, CancellationToken ct)
    {
        var entity = new CaseConference
        {
            Id             = Guid.NewGuid(),
            ResidentCaseId = request.ResidentCaseId,
            StatusStateId  = request.StatusStateId,
            ConferenceDate = request.ConferenceDate,
            OutcomeSummary = request.OutcomeSummary ?? string.Empty,
        };
        db.CaseConferences.Add(entity);
        await db.SaveChangesAsync(ct);
        await db.Entry(entity).Reference(x => x.StatusState).LoadAsync(ct);
        return new CaseConferenceItem(
            entity.Id, entity.ResidentCaseId, entity.ConferenceDate,
            entity.StatusState?.Name ?? "Unknown",
            entity.OutcomeSummary);
    }
}

public sealed class DonorContributionService(SafeHarborDbContext db) : IDonorContributionService
{
    public async Task<PagedResult<DonorListItem>> GetDonorsAsync(PagingQuery query, CancellationToken ct)
    {
        var q = db.Supporters.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            q = q.Where(x => x.DisplayName.ToLower().Contains(search) || x.Email.ToLower().Contains(search));
        }

        q = query.Desc ? q.OrderByDescending(x => x.LastActivityAt) : q.OrderBy(x => x.LastActivityAt);

        var total = await q.CountAsync(ct);
        var page = query.NormalizedPage;
        var pageSize = query.NormalizedPageSize;
        var items = await q.Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new DonorListItem(x.Id, x.DisplayName, x.Email, x.LastActivityAt, x.LifetimeDonations))
            .ToArrayAsync(ct);

        return new PagedResult<DonorListItem>(items, page, pageSize, total);
    }

    public async Task<DonorListItem> CreateDonorAsync(CreateDonorRequest request, CancellationToken ct)
    {
        var supporter = new Supporter
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            DisplayName = request.Name,
            Email = request.Email,
            LastActivityAt = DateTimeOffset.UtcNow,
            LifetimeDonations = 0m
        };

        db.Supporters.Add(supporter);
        await db.SaveChangesAsync(ct);
        return new DonorListItem(supporter.Id, supporter.DisplayName, supporter.Email, supporter.LastActivityAt, supporter.LifetimeDonations);
    }

    public async Task<ContributionListItem> CreateContributionAsync(CreateContributionRequest request, CancellationToken ct)
    {
        var supporter = await db.Supporters.FirstOrDefaultAsync(x => x.Id == request.DonorId, ct)
            ?? throw new KeyNotFoundException($"Supporter {request.DonorId} was not found.");

        var contribution = new Contribution
        {
            Id = Guid.NewGuid(),
            SupporterId = request.DonorId,
            Amount = request.Amount,
            CampaignId = request.CampaignId,
            ContributionDate = request.ContributionDate ?? DateTimeOffset.UtcNow,
            ContributionTypeId = request.ContributionTypeId,
            StatusStateId = request.StatusStateId
        };

        supporter.LifetimeDonations += request.Amount;
        supporter.LastActivityAt = DateTimeOffset.UtcNow;

        db.Contributions.Add(contribution);
        await db.SaveChangesAsync(ct);

        var statusName = await db.StatusState.AsNoTracking().Where(x => x.Id == request.StatusStateId).Select(x => x.Name).FirstOrDefaultAsync(ct) ?? "Unknown";
        return new ContributionListItem(contribution.Id, supporter.DisplayName, contribution.Amount, contribution.ContributionDate, statusName);
    }

    public async Task<bool> CreateAllocationAsync(CreateAllocationRequest request, CancellationToken ct)
    {
        var contributionExists = await db.Contributions.AnyAsync(x => x.Id == request.ContributionId, ct);
        var safehouseExists = await db.Safehouses.AnyAsync(x => x.Id == request.SafehouseId, ct);
        if (!contributionExists || !safehouseExists)
        {
            return false;
        }

        // Keep allocations explicit in the database so donation analytics can compare funding distribution by safehouse.
        db.Set<ContributionAllocation>().Add(new ContributionAllocation
        {
            Id = Guid.NewGuid(),
            ContributionId = request.ContributionId,
            SafehouseId = request.SafehouseId,
            AmountAllocated = request.AmountAllocated
        });

        await db.SaveChangesAsync(ct);
        return true;
    }
}

public sealed class ReportsAnalyticsService(SafeHarborDbContext db) : IReportsAnalyticsService
{
    private static readonly HashSet<string> ReintegrationSuccessStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "success",
        "successful",
        "completed",
        "stable",
        "reintegrated"
    };

    public async Task<ReportsAnalyticsResponse> GetAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var sixMonthsAgo = now.AddMonths(-5);

        var donations = await TryReadOptionalAnalyticsDataAsync(
            () => db.Contributions.AsNoTracking()
                .Where(x => x.ContributionDate >= sixMonthsAgo)
                // NOTE: Use a narrow projection so this report can keep serving when unrelated
                // columns are added in newer DB migrations that are not yet applied.
                .Select(x => new DonationAnalyticsRow(x.ContributionDate, x.Amount))
                .ToArrayAsync(ct));

        var visits = await TryReadOptionalAnalyticsDataAsync(
            () => db.HomeVisits.AsNoTracking()
                .Where(x => x.VisitDate >= sixMonthsAgo)
                .Select(x => new HomeVisitAnalyticsRow(x.VisitDate))
                .ToArrayAsync(ct));

        var residentCases = await TryReadOptionalAnalyticsDataAsync(
            () => db.ResidentCases.AsNoTracking()
                .Select(x => new ResidentCaseAnalyticsRow(
                    x.SafehouseId,
                    x.Safehouse != null ? x.Safehouse.Name : null,
                    x.OpenedAt,
                    x.ClosedAt))
                .ToArrayAsync(ct));
        var allocations = await TryReadOptionalAnalyticsDataAsync(
            () => db.Set<ContributionAllocation>().AsNoTracking()
                .Select(x => new ContributionAllocationAnalyticsRow(x.SafehouseId, x.AmountAllocated))
                .ToArrayAsync(ct));

        var donationTrends = donations
            .GroupBy(x => x.ContributionDate.ToString("yyyy-MM"))
            .OrderBy(x => x.Key)
            .Select(x => new DonationTrendPoint(x.Key, x.Sum(v => v.Amount)))
            .ToArray();
        if (donationTrends.Length == 0)
        {
            donationTrends = await LoadLegacyDonationTrendsAsync(ct);
        }

        var outcomeTrends = visits
            .GroupBy(x => x.VisitDate.ToString("yyyy-MM"))
            .OrderBy(x => x.Key)
            .Select(x => new OutcomeTrendPoint(x.Key, residentCases.Count(rc => rc.OpenedAt.ToString("yyyy-MM") == x.Key), x.Count()))
            .ToArray();
        if (outcomeTrends.Length == 0)
        {
            outcomeTrends = await LoadLegacyOutcomeTrendsAsync(ct);
        }

        var safehouseComparisons = residentCases
            .GroupBy(x => x.SafehouseId)
            .Select(g =>
            {
                var safehouseName = g.First().SafehouseName ?? "Unknown";
                var allocationTotal = allocations.Where(a => a.SafehouseId == g.Key).Sum(a => a.AmountAllocated);
                return new SafehouseComparisonItem(safehouseName, g.Count(x => x.ClosedAt == null), allocationTotal);
            })
            .OrderByDescending(x => x.AllocatedFunding)
            .ToArray();
        if (safehouseComparisons.Length == 0)
        {
            safehouseComparisons = await LoadLegacySafehouseComparisonsAsync(ct);
        }

        var posts = await TryReadOptionalAnalyticsDataAsync(
            () => db.SocialPostMetrics.AsNoTracking()
                .Select(x => new SocialPostAnalyticsRow(
                    x.Id,
                    x.PostedAt,
                    x.Platform,
                    x.ContentType,
                    x.Reach,
                    x.Engagements,
                    x.AttributedDonationAmount,
                    x.AttributedDonationCount))
                .ToArrayAsync(ct));

        SocialDonationCorrelationPoint[] platform;
        SocialDonationCorrelationPoint[] contentType;
        SocialDonationCorrelationPoint[] postingHour;
        SocialPostDonationInsight[] topPosts;

        if (posts.Length > 0)
        {
            platform = BuildCorrelation(posts, x => x.Platform);
            contentType = BuildCorrelation(posts, x => x.ContentType);
            postingHour = BuildCorrelation(posts, x => x.PostedAt.Hour.ToString("00") + ":00");
            topPosts = posts
                .OrderByDescending(x => x.AttributedDonationAmount ?? 0)
                .Take(5)
                .Select(x => new SocialPostDonationInsight(
                    x.Id,
                    x.PostedAt,
                    x.Platform,
                    x.ContentType,
                    x.Reach,
                    x.Engagements,
                    x.AttributedDonationAmount,
                    x.AttributedDonationCount,
                    x.Reach > 0 ? Math.Round((decimal)x.Engagements / x.Reach * 100m, 2) : 0m))
                .ToArray();
        }
        else
        {
            var legacySocialRows = await LoadLegacySocialRowsAsync(ct);
            platform = BuildCorrelation(legacySocialRows, x => x.Platform);
            contentType = BuildCorrelation(legacySocialRows, x => x.ContentType);
            postingHour = BuildCorrelation(legacySocialRows, x => x.PostedAt.Hour.ToString("00") + ":00");
            topPosts = legacySocialRows
                .OrderByDescending(x => x.AttributedDonationAmount ?? 0)
                .Take(5)
                .Select(x => new SocialPostDonationInsight(
                    x.Id,
                    x.PostedAt,
                    x.Platform,
                    x.ContentType,
                    x.Reach,
                    x.Engagements,
                    x.AttributedDonationAmount,
                    x.AttributedDonationCount,
                    x.Reach > 0 ? Math.Round((decimal)x.Engagements / x.Reach * 100m, 2) : 0m))
                .ToArray();
        }

        var reintegrationRates = await LoadLegacyReintegrationRatesAsync(ct);

        var recommendations = new List<ContentTimingRecommendationCard>();
        var bestPlatform = platform.OrderByDescending(x => x.TotalAttributedDonationAmount).FirstOrDefault();
        if (bestPlatform is not null)
        {
            recommendations.Add(new ContentTimingRecommendationCard(
                "Prioritize strongest platform",
                $"{bestPlatform.Group} currently drives the highest attributed donation volume.",
                "Increase post volume on that platform while validating attribution over the next month."));
        }

        return new ReportsAnalyticsResponse(
            donationTrends,
            outcomeTrends,
            safehouseComparisons,
            reintegrationRates,
            platform,
            contentType,
            postingHour,
            topPosts,
            recommendations);
    }

    // NOTE: Reports analytics is optional in partially-migrated environments.
    // Missing table/column errors should degrade to empty report slices instead of 500.
    private static async Task<T[]> TryReadOptionalAnalyticsDataAsync<T>(Func<Task<T[]>> query)
    {
        try
        {
            return await query();
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable || ex.SqlState == PostgresErrorCodes.UndefinedColumn)
        {
            return Array.Empty<T>();
        }
    }

    private static SocialDonationCorrelationPoint[] BuildCorrelation(IEnumerable<SocialPostAnalyticsRow> metrics, Func<SocialPostAnalyticsRow, string> groupBy)
    {
        return metrics
            .GroupBy(groupBy)
            .Select(g =>
            {
                var totalReach = g.Sum(x => x.Reach);
                var totalAttributed = g.Sum(x => x.AttributedDonationAmount ?? 0);
                var totalEngagements = g.Sum(x => x.Engagements);
                var donationCount = g.Sum(x => x.AttributedDonationCount ?? 0);
                var per1k = totalReach > 0 ? totalAttributed / (totalReach / 1000m) : 0m;
                var rate = totalReach > 0 ? (decimal)totalEngagements / totalReach * 100m : 0m;
                return new SocialDonationCorrelationPoint(g.Key, g.Count(), totalReach, totalEngagements, totalAttributed, donationCount, Math.Round(per1k, 2), Math.Round(rate, 2));
            })
            .OrderByDescending(x => x.TotalAttributedDonationAmount)
            .ToArray();
    }

    private async Task<DonationTrendPoint[]> LoadLegacyDonationTrendsAsync(CancellationToken ct)
    {
        var relation = await ResolveRelationAsync(["lighthouse.donations", "public.donations"], ct);
        if (relation is null)
        {
            return [];
        }

        var sql = $"""
            SELECT to_char(date_trunc('month', donation_date), 'YYYY-MM') AS month_key,
                   COALESCE(SUM(COALESCE(amount, 0)), 0) AS total_amount
            FROM {relation}
            WHERE donation_date IS NOT NULL
            GROUP BY 1
            ORDER BY 1
            """;

        return await ExecuteReadAsync(sql, reader =>
        {
            var month = reader.GetString(0);
            var amount = reader.IsDBNull(1) ? 0m : reader.GetDecimal(1);
            return new DonationTrendPoint(month, amount);
        }, ct);
    }

    private async Task<OutcomeTrendPoint[]> LoadLegacyOutcomeTrendsAsync(CancellationToken ct)
    {
        var metricsRelation = await ResolveRelationAsync(["lighthouse.safehouse_monthly_metrics", "public.safehouse_monthly_metrics"], ct);
        if (metricsRelation is not null)
        {
            var sql = $"""
                SELECT to_char(month_start, 'YYYY-MM') AS month_key,
                       COALESCE(SUM(active_residents), 0) AS residents_served,
                       COALESCE(SUM(home_visitation_count), 0) AS home_visit_count
                FROM {metricsRelation}
                WHERE month_start IS NOT NULL
                GROUP BY 1
                ORDER BY 1
                """;

            return await ExecuteReadAsync(sql, reader => new OutcomeTrendPoint(
                reader.GetString(0),
                reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                reader.IsDBNull(2) ? 0 : reader.GetInt32(2)), ct);
        }

        var visitRelation = await ResolveRelationAsync(["lighthouse.home_visitations", "public.home_visitations"], ct);
        if (visitRelation is null)
        {
            return [];
        }

        var fallbackSql = $"""
            SELECT to_char(date_trunc('month', visit_date), 'YYYY-MM') AS month_key,
                   0 AS residents_served,
                   COUNT(*)::int AS home_visit_count
            FROM {visitRelation}
            WHERE visit_date IS NOT NULL
            GROUP BY 1
            ORDER BY 1
            """;

        return await ExecuteReadAsync(fallbackSql, reader => new OutcomeTrendPoint(
            reader.GetString(0),
            reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
            reader.IsDBNull(2) ? 0 : reader.GetInt32(2)), ct);
    }

    private async Task<SafehouseComparisonItem[]> LoadLegacySafehouseComparisonsAsync(CancellationToken ct)
    {
        var metricsRelation = await ResolveRelationAsync(["lighthouse.safehouse_monthly_metrics", "public.safehouse_monthly_metrics"], ct);
        var safehouseRelation = await ResolveRelationAsync(["lighthouse.safehouses", "public.safehouses"], ct);
        if (metricsRelation is null || safehouseRelation is null)
        {
            return [];
        }

        var sql = $"""
            WITH latest AS (
                SELECT MAX(month_start) AS latest_month
                FROM {metricsRelation}
            )
            SELECT
                COALESCE(s.name, 'Unknown') AS safehouse_name,
                COALESCE(m.active_residents, 0) AS active_residents,
                0::numeric AS allocated_funding
            FROM {metricsRelation} m
            JOIN latest l ON m.month_start = l.latest_month
            LEFT JOIN {safehouseRelation} s ON s.safehouse_id = m.safehouse_id
            ORDER BY active_residents DESC
            """;

        return await ExecuteReadAsync(sql, reader => new SafehouseComparisonItem(
            reader.GetString(0),
            reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
            reader.IsDBNull(2) ? 0m : reader.GetDecimal(2)), ct);
    }

    private async Task<ReintegrationRatePoint[]> LoadLegacyReintegrationRatesAsync(CancellationToken ct)
    {
        var residentRelation = await ResolveRelationAsync(["lighthouse.residents", "public.residents"], ct);
        if (residentRelation is null)
        {
            return [];
        }

        var sql = $"""
            SELECT
                to_char(date_trunc('month', date_closed), 'YYYY-MM') AS month_key,
                COALESCE(
                    100.0 * SUM(
                        CASE WHEN lower(COALESCE(reintegration_status, '')) = ANY(@success_statuses) THEN 1 ELSE 0 END
                    ) / NULLIF(COUNT(*), 0),
                    0
                ) AS rate_percent
            FROM {residentRelation}
            WHERE date_closed IS NOT NULL
            GROUP BY 1
            ORDER BY 1
            """;

        var parameters = new Dictionary<string, object>
        {
            ["success_statuses"] = ReintegrationSuccessStatuses.Select(x => x.ToLowerInvariant()).ToArray()
        };

        return await ExecuteReadAsync(sql, reader => new ReintegrationRatePoint(
            reader.GetString(0),
            reader.IsDBNull(1) ? 0m : Math.Round(reader.GetDecimal(1), 2)), ct, parameters);
    }

    private async Task<SocialPostAnalyticsRow[]> LoadLegacySocialRowsAsync(CancellationToken ct)
    {
        var relation = await ResolveRelationAsync(["lighthouse.social_media_posts", "public.social_media_posts"], ct);
        if (relation is null)
        {
            return [];
        }

        var sql = $"""
            SELECT
                post_id,
                COALESCE(created_at, CURRENT_TIMESTAMP),
                COALESCE(platform, 'Unknown'),
                COALESCE(post_type, 'Unknown'),
                COALESCE(reach, 0),
                COALESCE(likes, 0) + COALESCE(comments, 0) + COALESCE(shares, 0) + COALESCE(saves, 0) + COALESCE(click_throughs, 0),
                COALESCE(estimated_donation_value_php, 0),
                COALESCE(donation_referrals, 0)
            FROM {relation}
            """;

        return await ExecuteReadAsync(sql, reader =>
        {
            var legacyPostId = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
            var postedAt = reader.IsDBNull(1)
                ? DateTimeOffset.UtcNow
                : DateTime.SpecifyKind(reader.GetDateTime(1), DateTimeKind.Utc);
            var platform = reader.GetString(2);
            var contentType = reader.GetString(3);
            var reach = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
            var engagements = reader.IsDBNull(5) ? 0 : reader.GetInt32(5);
            var amount = reader.IsDBNull(6) ? 0m : reader.GetDecimal(6);
            var referrals = reader.IsDBNull(7) ? 0 : reader.GetInt32(7);

            // Keep deterministic IDs for UI keys when source data comes from legacy integer post IDs.
            var normalizedGuid = Guid.TryParseExact($"00000000-0000-0000-0000-{legacyPostId:000000000000}", "D", out var parsed)
                ? parsed
                : Guid.Empty;

            return new SocialPostAnalyticsRow(normalizedGuid, postedAt, platform, contentType, reach, engagements, amount, referrals);
        }, ct);
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

    private sealed record DonationAnalyticsRow(DateTimeOffset ContributionDate, decimal Amount);
    private sealed record HomeVisitAnalyticsRow(DateTimeOffset VisitDate);
    private sealed record ResidentCaseAnalyticsRow(Guid SafehouseId, string? SafehouseName, DateTimeOffset OpenedAt, DateTimeOffset? ClosedAt);
    private sealed record ContributionAllocationAnalyticsRow(Guid SafehouseId, decimal AmountAllocated);
    private sealed record SocialPostAnalyticsRow(
        Guid Id,
        DateTimeOffset PostedAt,
        string Platform,
        string ContentType,
        int Reach,
        int Engagements,
        decimal? AttributedDonationAmount,
        int? AttributedDonationCount);
}


