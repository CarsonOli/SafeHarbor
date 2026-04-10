using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SafeHarbor.Data;
using SafeHarbor.DTOs;

namespace SafeHarbor.Services.Donations;

public interface IDonationAccessService
{
    Task<PagedResult<DonationListItem>> GetAllDonationsAsync(DonationFiltersQuery filters, CancellationToken ct);
    Task<DonationListItem?> GetDonationByIdAsync(long donationId, CancellationToken ct);
    Task<YourDonationsResponse> GetCurrentUserDonationsAsync(Guid userId, CancellationToken ct);
    Task<bool> LinkUserToSupporterAsync(Guid userId, long supporterId, CancellationToken ct);
    Task<long?> FindSupporterByEmailAsync(string email, CancellationToken ct);
    Task<long?> EnsureSupporterForEmailAsync(string email, string? firstName, string? lastName, CancellationToken ct);
}

public sealed class DonationAccessService(SafeHarborDbContext dbContext) : IDonationAccessService
{
    public async Task<PagedResult<DonationListItem>> GetAllDonationsAsync(DonationFiltersQuery filters, CancellationToken ct)
    {
        var normalized = NormalizeFilters(filters);
        var sql = BuildDonationQuery(normalized, scopedSupporterId: null);
        var totalSql = BuildDonationCountQuery(normalized, scopedSupporterId: null);

        await using var conn = await OpenConnectionAsync(ct);
        var totalCount = await ExecuteScalarAsync<int>(conn, totalSql.Sql, totalSql.Parameters, ct);
        var donations = await ReadDonationsAsync(conn, sql.Sql, sql.Parameters, ct);

        return new PagedResult<DonationListItem>(donations, normalized.Page, normalized.PageSize, totalCount);
    }

    public async Task<DonationListItem?> GetDonationByIdAsync(long donationId, CancellationToken ct)
    {
        var query = BuildDonationByIdQuery(donationId);
        await using var conn = await OpenConnectionAsync(ct);
        var items = await ReadDonationsAsync(conn, query.Sql, query.Parameters, ct);
        return items.FirstOrDefault();
    }

    public async Task<YourDonationsResponse> GetCurrentUserDonationsAsync(Guid userId, CancellationToken ct)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, ct);

        if (user?.SupporterId is null)
        {
            return new YourDonationsResponse(false, null, null, []);
        }

        var scopedFilters = new DonationFiltersQuery(null, null, null, null, null, null, null, 1, 100);
        var query = BuildDonationQuery(NormalizeFilters(scopedFilters), user.SupporterId.Value);
        await using var conn = await OpenConnectionAsync(ct);
        var donations = await ReadDonationsAsync(conn, query.Sql, query.Parameters, ct);

        var supporterDisplayName = donations.FirstOrDefault()?.DonorDisplayName;
        return new YourDonationsResponse(true, user.SupporterId, supporterDisplayName, donations);
    }

    public async Task<bool> LinkUserToSupporterAsync(Guid userId, long supporterId, CancellationToken ct)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (user is null)
        {
            return false;
        }

        var supporterExists = await RelationRowExistsAsync("lighthouse", "supporters", "supporter_id", supporterId, ct);
        if (!supporterExists)
        {
            return false;
        }

        user.SupporterId = supporterId;
        user.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<long?> FindSupporterByEmailAsync(string email, CancellationToken ct)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return null;
        }

        if (!await RelationExistsAsync("lighthouse", "supporters", ct))
        {
            return null;
        }

        await using var conn = await OpenConnectionAsync(ct);
        const string sql = """
            SELECT supporter_id
            FROM lighthouse.supporters
            WHERE lower(email) = @email
            LIMIT 1
            """;

        var value = await ExecuteScalarAsync<object?>(conn, sql, new Dictionary<string, object> { ["email"] = normalizedEmail }, ct);
        return value is null || value is DBNull ? null : Convert.ToInt64(value);
    }

    public async Task<long?> EnsureSupporterForEmailAsync(string email, string? firstName, string? lastName, CancellationToken ct)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return null;
        }

        if (!await RelationExistsAsync("lighthouse", "supporters", ct))
        {
            return null;
        }

        var existing = await FindSupporterByEmailAsync(normalizedEmail, ct);
        if (existing is not null)
        {
            return existing;
        }

        // NOTE: We always create a CRM supporter profile before linking auth users so donation
        // ownership stays tied to supporters, not directly to auth account rows.
        // Some environments define supporters.supporter_id without a default sequence, so we
        // defensively generate an ID when needed.
        var fallbackName = BuildSupporterDisplayName(normalizedEmail, firstName, lastName);
        await using var conn = await OpenConnectionAsync(ct);
        const string insertSql = """
            WITH resolved_id AS (
                SELECT CASE
                    WHEN pg_get_serial_sequence('lighthouse.supporters', 'supporter_id') IS NOT NULL
                        THEN nextval(pg_get_serial_sequence('lighthouse.supporters', 'supporter_id'))
                    ELSE (
                        SELECT COALESCE(MAX(supporter_id), 0) + 1
                        FROM lighthouse.supporters
                    )
                END AS supporter_id
            )
            INSERT INTO lighthouse.supporters (supporter_id, display_name, first_name, last_name, email, supporter_type)
            SELECT
                resolved_id.supporter_id,
                @display_name,
                @first_name,
                @last_name,
                @email,
                @supporter_type
            FROM resolved_id
            RETURNING supporter_id
            """;

        try
        {
            var created = await ExecuteScalarAsync<object?>(
                conn,
                insertSql,
                new Dictionary<string, object>
                {
                    ["display_name"] = fallbackName,
                    ["first_name"] = string.IsNullOrWhiteSpace(firstName) ? DBNull.Value : firstName.Trim(),
                    ["last_name"] = string.IsNullOrWhiteSpace(lastName) ? DBNull.Value : lastName.Trim(),
                    ["email"] = normalizedEmail,
                    ["supporter_type"] = "Individual",
                },
                ct);

            return created is null || created is DBNull ? null : Convert.ToInt64(created);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            // Concurrent registration can race the insert for the same email; resolve by
            // reading the supporter row that just won the unique constraint.
            return await FindSupporterByEmailAsync(normalizedEmail, ct);
        }
    }

    private async Task<IReadOnlyCollection<DonationListItem>> ReadDonationsAsync(
        NpgsqlConnection conn,
        string sql,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct)
    {
        var grouped = new Dictionary<long, DonationAggregateRow>();

        await using var cmd = new NpgsqlCommand(sql, conn);
        AddParameters(cmd, parameters);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var donationId = reader.GetInt64(0);
            if (!grouped.TryGetValue(donationId, out var aggregate))
            {
                aggregate = new DonationAggregateRow(
                    donationId,
                    GetDateTimeOffsetOrNull(reader, 1),
                    GetStringOrEmpty(reader, 2),
                    GetDecimalOrZero(reader, 3),
                    GetDecimalOrZero(reader, 4),
                    GetStringOrNull(reader, 5),
                    GetStringOrNull(reader, 6),
                    GetStringOrNull(reader, 7),
                    GetStringOrNull(reader, 8),
                    reader.GetInt64(9),
                    BuildDonorDisplayName(GetStringOrNull(reader, 10), GetStringOrNull(reader, 11), GetStringOrNull(reader, 12), GetStringOrNull(reader, 13)),
                    GetStringOrNull(reader, 14),
                    GetStringOrNull(reader, 15),
                    []);
                grouped[donationId] = aggregate;
            }

            if (!reader.IsDBNull(16))
            {
                aggregate.InKindItems.Add(new InKindDonationItemDto(
                    reader.GetInt64(16),
                    GetStringOrNull(reader, 17),
                    GetStringOrNull(reader, 18),
                    GetDecimalOrZero(reader, 19),
                    GetStringOrNull(reader, 20),
                    GetDecimalOrZero(reader, 21)));
            }
        }

        return grouped.Values
            .OrderByDescending(x => x.DonationDate ?? DateTimeOffset.MinValue)
            .Select(x => new DonationListItem(
                x.DonationId,
                x.DonationDate,
                x.DonationType,
                x.Amount,
                x.EstimatedValue,
                x.CampaignName,
                x.ChannelSource,
                x.Frequency,
                x.Notes,
                x.SupporterId,
                x.DonorDisplayName,
                x.SupporterType,
                x.SupporterEmail,
                x.InKindItems))
            .ToArray();
    }

    private static (string Sql, IReadOnlyDictionary<string, object> Parameters) BuildDonationByIdQuery(long donationId)
    {
        var sql = BaseDonationProjection() + "\nWHERE d.donation_id = @donation_id\nORDER BY d.donation_date DESC, i.item_id";
        return (sql, new Dictionary<string, object> { ["donation_id"] = donationId });
    }

    private static (string Sql, IReadOnlyDictionary<string, object> Parameters) BuildDonationQuery(NormalizedDonationFilters filters, long? scopedSupporterId)
    {
        var where = new List<string>();
        var parameters = new Dictionary<string, object>();

        if (filters.FromDate is { } from)
        {
            where.Add("d.donation_date >= @from_date");
            parameters["from_date"] = from;
        }

        if (filters.ToDate is { } to)
        {
            where.Add("d.donation_date <= @to_date");
            parameters["to_date"] = to;
        }

        AddTextFilter(where, parameters, "d.donation_type", "donation_type", filters.DonationType);
        AddTextFilter(where, parameters, "d.campaign_name", "campaign_name", filters.Campaign);
        AddTextFilter(where, parameters, "d.channel_source", "channel_source", filters.ChannelSource);
        AddTextFilter(where, parameters, "s.supporter_type", "supporter_type", filters.SupporterType);
        AddTextFilter(where, parameters, "d.frequency", "frequency", filters.Frequency);

        if (scopedSupporterId is { } supporterId)
        {
            where.Add("d.supporter_id = @supporter_id");
            parameters["supporter_id"] = supporterId;
        }

        var whereClause = where.Count > 0 ? $"WHERE {string.Join(" AND ", where)}" : string.Empty;
        parameters["limit"] = filters.PageSize;
        parameters["offset"] = (filters.Page - 1) * filters.PageSize;

        var sql = $$"""
            {{BaseDonationProjection()}}
            {{whereClause}}
            ORDER BY d.donation_date DESC NULLS LAST, d.donation_id DESC, i.item_id
            LIMIT @limit OFFSET @offset
            """;

        return (sql, parameters);
    }

    private static (string Sql, IReadOnlyDictionary<string, object> Parameters) BuildDonationCountQuery(NormalizedDonationFilters filters, long? scopedSupporterId)
    {
        var where = new List<string>();
        var parameters = new Dictionary<string, object>();

        if (filters.FromDate is { } from)
        {
            where.Add("d.donation_date >= @from_date");
            parameters["from_date"] = from;
        }

        if (filters.ToDate is { } to)
        {
            where.Add("d.donation_date <= @to_date");
            parameters["to_date"] = to;
        }

        AddTextFilter(where, parameters, "d.donation_type", "donation_type", filters.DonationType);
        AddTextFilter(where, parameters, "d.campaign_name", "campaign_name", filters.Campaign);
        AddTextFilter(where, parameters, "d.channel_source", "channel_source", filters.ChannelSource);
        AddTextFilter(where, parameters, "s.supporter_type", "supporter_type", filters.SupporterType);
        AddTextFilter(where, parameters, "d.frequency", "frequency", filters.Frequency);

        if (scopedSupporterId is { } supporterId)
        {
            where.Add("d.supporter_id = @supporter_id");
            parameters["supporter_id"] = supporterId;
        }

        var whereClause = where.Count > 0 ? $"WHERE {string.Join(" AND ", where)}" : string.Empty;
        var sql = $$"""
            SELECT COUNT(*)
            FROM lighthouse.donations d
            JOIN lighthouse.supporters s ON s.supporter_id = d.supporter_id
            {{whereClause}}
            """;

        return (sql, parameters);
    }

    private static string BaseDonationProjection() =>
        """
        SELECT
            d.donation_id,
            d.donation_date,
            COALESCE(d.donation_type, 'Unknown') AS donation_type,
            COALESCE(d.amount, 0) AS amount,
            COALESCE(d.estimated_value, 0) AS estimated_value,
            d.campaign_name,
            d.channel_source,
            d.frequency,
            d.notes,
            d.supporter_id,
            s.display_name,
            s.organization_name,
            s.first_name,
            s.last_name,
            s.supporter_type,
            s.email,
            i.item_id,
            i.item_name,
            i.item_category,
            COALESCE(i.quantity, 0) AS quantity,
            i.unit_of_measure,
            COALESCE(i.estimated_unit_value, 0) AS estimated_unit_value
        FROM lighthouse.donations d
        JOIN lighthouse.supporters s ON s.supporter_id = d.supporter_id
        LEFT JOIN lighthouse.in_kind_donation_items i ON i.donation_id = d.donation_id
        """;

    private static string BuildSupporterDisplayName(string normalizedEmail, string? firstName, string? lastName)
    {
        var fullName = $"{firstName} {lastName}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        var local = normalizedEmail.Split('@', 2, StringSplitOptions.TrimEntries)[0];
        return string.IsNullOrWhiteSpace(local) ? normalizedEmail : local;
    }

    private static string BuildDonorDisplayName(string? displayName, string? organizationName, string? firstName, string? lastName)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName;
        }

        var personName = $"{firstName} {lastName}".Trim();
        if (!string.IsNullOrWhiteSpace(personName))
        {
            return personName;
        }

        if (!string.IsNullOrWhiteSpace(organizationName))
        {
            return organizationName;
        }

        return "Unknown supporter";
    }

    private static void AddTextFilter(List<string> where, Dictionary<string, object> parameters, string field, string parameterName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        where.Add($"{field} ILIKE @{parameterName}");
        parameters[parameterName] = $"%{value.Trim()}%";
    }

    private static NormalizedDonationFilters NormalizeFilters(DonationFiltersQuery filters) =>
        new(
            filters.FromDate,
            filters.ToDate,
            NormalizeText(filters.DonationType),
            NormalizeText(filters.Campaign),
            NormalizeText(filters.ChannelSource),
            NormalizeText(filters.SupporterType),
            NormalizeText(filters.Frequency),
            Math.Max(1, filters.Page),
            Math.Clamp(filters.PageSize, 1, 200));

    private static string? NormalizeText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken ct)
    {
        var connString = dbContext.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connString))
        {
            throw new InvalidOperationException("Database connection string is not configured.");
        }

        var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync(ct);
        return conn;
    }

    private static async Task<T> ExecuteScalarAsync<T>(
        NpgsqlConnection conn,
        string sql,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        AddParameters(cmd, parameters);
        var result = await cmd.ExecuteScalarAsync(ct);
        if (result is null || result is DBNull)
        {
            return default!;
        }

        return (T)Convert.ChangeType(result, typeof(T));
    }

    private static void AddParameters(NpgsqlCommand cmd, IReadOnlyDictionary<string, object> parameters)
    {
        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
    }

    private async Task<bool> RelationExistsAsync(string schema, string table, CancellationToken ct)
    {
        await using var conn = await OpenConnectionAsync(ct);
        const string sql = """
            SELECT EXISTS (
              SELECT 1
              FROM information_schema.tables
              WHERE table_schema = @schema AND table_name = @table
            )
            """;

        return await ExecuteScalarAsync<bool>(
            conn,
            sql,
            new Dictionary<string, object> { ["schema"] = schema, ["table"] = table },
            ct);
    }

    private async Task<bool> RelationRowExistsAsync(string schema, string table, string idColumn, long idValue, CancellationToken ct)
    {
        await using var conn = await OpenConnectionAsync(ct);
        var sql = $"""
            SELECT EXISTS (
              SELECT 1
              FROM {schema}.{table}
              WHERE {idColumn} = @id_value
            )
            """;

        return await ExecuteScalarAsync<bool>(
            conn,
            sql,
            new Dictionary<string, object> { ["id_value"] = idValue },
            ct);
    }

    private static DateTimeOffset? GetDateTimeOffsetOrNull(IDataRecord reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        var value = reader.GetValue(ordinal);
        return value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            _ => null
        };
    }

    private static decimal GetDecimalOrZero(IDataRecord reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return 0m;
        }

        return Convert.ToDecimal(reader.GetValue(ordinal));
    }

    private static string? GetStringOrNull(IDataRecord reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static string GetStringOrEmpty(IDataRecord reader, int ordinal) => reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);

    private sealed record NormalizedDonationFilters(
        DateTimeOffset? FromDate,
        DateTimeOffset? ToDate,
        string? DonationType,
        string? Campaign,
        string? ChannelSource,
        string? SupporterType,
        string? Frequency,
        int Page,
        int PageSize);

    private sealed class DonationAggregateRow(
        long donationId,
        DateTimeOffset? donationDate,
        string donationType,
        decimal amount,
        decimal estimatedValue,
        string? campaignName,
        string? channelSource,
        string? frequency,
        string? notes,
        long supporterId,
        string donorDisplayName,
        string? supporterType,
        string? supporterEmail,
        List<InKindDonationItemDto> inKindItems)
    {
        public long DonationId { get; } = donationId;
        public DateTimeOffset? DonationDate { get; } = donationDate;
        public string DonationType { get; } = donationType;
        public decimal Amount { get; } = amount;
        public decimal EstimatedValue { get; } = estimatedValue;
        public string? CampaignName { get; } = campaignName;
        public string? ChannelSource { get; } = channelSource;
        public string? Frequency { get; } = frequency;
        public string? Notes { get; } = notes;
        public long SupporterId { get; } = supporterId;
        public string DonorDisplayName { get; } = donorDisplayName;
        public string? SupporterType { get; } = supporterType;
        public string? SupporterEmail { get; } = supporterEmail;
        public List<InKindDonationItemDto> InKindItems { get; } = inKindItems;
    }
}
