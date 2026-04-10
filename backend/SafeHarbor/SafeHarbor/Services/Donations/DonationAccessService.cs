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
    Task<long?> CreateDonationForCurrentUserAsync(Guid userId, string? email, decimal amount, string? frequency, CancellationToken ct);
    Task<bool> LinkUserToSupporterAsync(Guid userId, long supporterId, CancellationToken ct);
    Task<long?> FindSupporterByEmailAsync(string email, CancellationToken ct);
    Task<long?> EnsureSupporterForEmailAsync(string email, string? firstName, string? lastName, CancellationToken ct);
}

public sealed class DonationAccessService(
    SafeHarborDbContext dbContext,
    ILogger<DonationAccessService> logger) : IDonationAccessService
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

        await using var conn = await OpenConnectionAsync(ct);
        var hasFrequencyColumn = await ColumnExistsAsync(conn, "lighthouse", "donations", "frequency", ct);
        var query = BuildCurrentUserDonationHistoryQuery(user.SupporterId.Value, hasFrequencyColumn);
        var donations = await ReadDonationsAsync(conn, query.Sql, query.Parameters, ct);

        var supporterDisplayName = donations.FirstOrDefault()?.DonorDisplayName;
        return new YourDonationsResponse(true, user.SupporterId, supporterDisplayName, donations);
    }

    public async Task<long?> CreateDonationForCurrentUserAsync(Guid userId, string? email, decimal amount, string? frequency, CancellationToken ct)
    {
        if (amount <= 0)
        {
            return null;
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (user is null)
        {
            return null;
        }

        var supporterId = user.SupporterId;
        if (supporterId is null && !string.IsNullOrWhiteSpace(email))
        {
            supporterId = await EnsureSupporterForEmailAsync(email, user.FirstName, user.LastName, ct);
            if (supporterId is not null)
            {
                user.SupporterId = supporterId;
                user.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(ct);
            }
        }

        if (supporterId is null)
        {
            return null;
        }

        await using var conn = await OpenConnectionAsync(ct);
        try
        {
            var insertPlan = await BuildDynamicInsertPlanAsync(
                conn,
                table: "donations",
                idColumn: "donation_id",
                knownValuesFactory: (column) => ResolveDonationColumnValue(
                    column,
                    supporterId.Value,
                    amount,
                    frequency),
                ct);

            var donationId = await ExecuteScalarAsync<object?>(
                conn,
                insertPlan.Sql,
                insertPlan.Parameters,
                ct);

            return donationId is null || donationId is DBNull ? null : Convert.ToInt64(donationId);
        }
        catch (PostgresException ex)
        {
            logger.LogError(
                ex,
                "Unable to create supporter-linked donation for user {UserId}. SQL state: {SqlState}",
                userId,
                ex.SqlState);
            return null;
        }
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
        var fallbackName = BuildSupporterDisplayName(normalizedEmail, firstName, lastName);
        await using var conn = await OpenConnectionAsync(ct);

        try
        {
            var insertPlan = await BuildDynamicInsertPlanAsync(
                conn,
                table: "supporters",
                idColumn: "supporter_id",
                knownValuesFactory: (column) => ResolveSupporterColumnValue(
                    column,
                    normalizedEmail,
                    fallbackName,
                    firstName,
                    lastName),
                ct);

            var created = await ExecuteScalarAsync<object?>(
                conn,
                insertPlan.Sql,
                insertPlan.Parameters,
                ct);

            return created is null || created is DBNull ? null : Convert.ToInt64(created);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            // Concurrent registration can race the insert for the same email; resolve by
            // reading the supporter row that just won the unique constraint.
            return await FindSupporterByEmailAsync(normalizedEmail, ct);
        }
        catch (PostgresException ex)
        {
            logger.LogError(
                ex,
                "Unable to auto-create supporter profile for {Email}. SQL state: {SqlState}",
                normalizedEmail,
                ex.SqlState);
            return null;
        }
    }

    private async Task<DynamicInsertPlan> BuildDynamicInsertPlanAsync(
        NpgsqlConnection conn,
        string table,
        string idColumn,
        Func<ColumnSchema, ColumnValueResolution> knownValuesFactory,
        CancellationToken ct)
    {
        var columns = await GetTableColumnsAsync(conn, table, ct);
        if (columns.Count == 0)
        {
            throw new InvalidOperationException($"Table lighthouse.{table} has no discoverable columns.");
        }

        var insertColumns = new List<string>();
        var insertExpressions = new List<string>();
        var parameters = new Dictionary<string, object>();
        var includeResolvedId = false;
        var paramCounter = 0;

        foreach (var column in columns)
        {
            if (string.Equals(column.Name, idColumn, StringComparison.OrdinalIgnoreCase))
            {
                includeResolvedId = true;
                insertColumns.Add(QuoteIdentifier(column.Name));
                insertExpressions.Add("(SELECT generated_id FROM resolved_id)");
                continue;
            }

            var knownValue = knownValuesFactory(column);
            if (knownValue.HasValue)
            {
                var paramName = $"p{paramCounter++}";
                insertColumns.Add(QuoteIdentifier(column.Name));
                insertExpressions.Add($"@{paramName}");
                parameters[paramName] = knownValue.Value ?? DBNull.Value;
                continue;
            }

            if (column.IsRequired)
            {
                var paramName = $"p{paramCounter++}";
                insertColumns.Add(QuoteIdentifier(column.Name));
                insertExpressions.Add($"@{paramName}");
                parameters[paramName] = BuildGenericDefault(column);
            }
        }

        if (insertColumns.Count == 0)
        {
            throw new InvalidOperationException($"No insertable columns were resolved for lighthouse.{table}.");
        }

        var sql = includeResolvedId
            ? $$"""
                WITH resolved_id AS (
                    SELECT CASE
                        WHEN pg_get_serial_sequence('lighthouse.{{table}}', '{{idColumn}}') IS NOT NULL
                            THEN nextval(pg_get_serial_sequence('lighthouse.{{table}}', '{{idColumn}}'))
                        ELSE (
                            SELECT COALESCE(MAX({{QuoteIdentifier(idColumn)}}), 0) + 1
                            FROM lighthouse.{{QuoteIdentifier(table)}}
                        )
                    END AS generated_id
                )
                INSERT INTO lighthouse.{{QuoteIdentifier(table)}} ({{string.Join(", ", insertColumns)}})
                VALUES ({{string.Join(", ", insertExpressions)}})
                RETURNING {{QuoteIdentifier(idColumn)}}
                """
            : $$"""
                INSERT INTO lighthouse.{{QuoteIdentifier(table)}} ({{string.Join(", ", insertColumns)}})
                VALUES ({{string.Join(", ", insertExpressions)}})
                RETURNING {{QuoteIdentifier(idColumn)}}
                """;

        return new DynamicInsertPlan(sql, parameters);
    }

    private static ColumnValueResolution ResolveSupporterColumnValue(
        ColumnSchema column,
        string normalizedEmail,
        string fallbackDisplayName,
        string? firstName,
        string? lastName)
    {
        var normalizedName = column.Name.ToLowerInvariant();
        return normalizedName switch
        {
            "display_name" => ColumnValueResolution.WithValue(fallbackDisplayName),
            "first_name" => string.IsNullOrWhiteSpace(firstName)
                ? ColumnValueResolution.WithNoValue()
                : ColumnValueResolution.WithValue(firstName.Trim()),
            "last_name" => string.IsNullOrWhiteSpace(lastName)
                ? ColumnValueResolution.WithNoValue()
                : ColumnValueResolution.WithValue(lastName.Trim()),
            "email" => ColumnValueResolution.WithValue(normalizedEmail),
            "supporter_type" => ColumnValueResolution.WithValue("Individual"),
            "relationship_type" => ColumnValueResolution.WithValue("Donor"),
            "status" => ColumnValueResolution.WithValue("Active"),
            "acquisition_channel" => ColumnValueResolution.WithValue("Web"),
            "country" => ColumnValueResolution.WithValue("Unknown"),
            "region" => ColumnValueResolution.WithValue("Unknown"),
            "organization_name" => ColumnValueResolution.WithValue(string.Empty),
            "phone" => ColumnValueResolution.WithValue(string.Empty),
            "created_at" => ColumnValueResolution.WithValue(DateTime.UtcNow),
            "updated_at" => ColumnValueResolution.WithValue(DateTime.UtcNow),
            "first_donation_date" => ColumnValueResolution.WithValue(DateTime.UtcNow.Date),
            _ => ColumnValueResolution.WithNoValue(),
        };
    }

    private static ColumnValueResolution ResolveDonationColumnValue(
        ColumnSchema column,
        long supporterId,
        decimal amount,
        string? frequency)
    {
        var normalizedName = column.Name.ToLowerInvariant();
        var recurring = string.Equals(frequency?.Trim(), "monthly", StringComparison.OrdinalIgnoreCase);
        return normalizedName switch
        {
            "supporter_id" => ColumnValueResolution.WithValue(supporterId),
            "donation_date" => ColumnValueResolution.WithValue(DateTime.UtcNow),
            "donation_type" => ColumnValueResolution.WithValue("Monetary"),
            "amount" => ColumnValueResolution.WithValue(amount),
            "estimated_value" => ColumnValueResolution.WithValue(0m),
            "campaign_name" => ColumnValueResolution.WithValue("Direct Web Donation"),
            "channel_source" => ColumnValueResolution.WithValue("Web"),
            "currency_code" => ColumnValueResolution.WithValue("USD"),
            "frequency" => ColumnValueResolution.WithValue(string.IsNullOrWhiteSpace(frequency) ? "One-time" : frequency.Trim()),
            "is_recurring" => column.DataType == "boolean"
                ? ColumnValueResolution.WithValue(recurring)
                : ColumnValueResolution.WithValue(recurring ? "True" : "False"),
            "notes" => ColumnValueResolution.WithValue("Submitted via donor portal"),
            "status" => ColumnValueResolution.WithValue("Completed"),
            "created_at" => ColumnValueResolution.WithValue(DateTime.UtcNow),
            "updated_at" => ColumnValueResolution.WithValue(DateTime.UtcNow),
            _ => ColumnValueResolution.WithNoValue(),
        };
    }

    private static object BuildGenericDefault(ColumnSchema column)
    {
        return column.DataType switch
        {
            "boolean" => false,
            "smallint" or "integer" => 0,
            "bigint" => 0L,
            "numeric" or "decimal" or "real" or "double precision" or "money" => 0m,
            "date" => DateTime.UtcNow.Date,
            "timestamp without time zone" or "timestamp with time zone" => DateTime.UtcNow,
            "uuid" => Guid.NewGuid(),
            "json" or "jsonb" => "{}",
            _ => string.Empty,
        };
    }

    private async Task<IReadOnlyList<ColumnSchema>> GetTableColumnsAsync(NpgsqlConnection conn, string table, CancellationToken ct)
    {
        const string sql = """
            SELECT column_name, is_nullable, column_default, is_identity, data_type
            FROM information_schema.columns
            WHERE table_schema = 'lighthouse' AND table_name = @table
            ORDER BY ordinal_position
            """;

        var columns = new List<ColumnSchema>();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("table", table);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var columnName = reader.GetString(0);
            var isNullable = string.Equals(reader.GetString(1), "YES", StringComparison.OrdinalIgnoreCase);
            var hasDefault = !reader.IsDBNull(2);
            var isIdentity = !reader.IsDBNull(3) && string.Equals(reader.GetString(3), "YES", StringComparison.OrdinalIgnoreCase);
            var dataType = reader.IsDBNull(4) ? "text" : reader.GetString(4);
            columns.Add(new ColumnSchema(columnName, dataType, isNullable, hasDefault, isIdentity));
        }

        return columns;
    }

    private static string QuoteIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";

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

    private static (string Sql, IReadOnlyDictionary<string, object> Parameters) BuildCurrentUserDonationHistoryQuery(
        long supporterId,
        bool hasFrequencyColumn)
    {
        // The "Your Donations" page is an account ledger view, so it must return complete
        // supporter history rather than paginated admin-style slices.
        // NOTE: Some production datasets are missing donations.frequency; emit NULL when absent.
        var frequencyProjection = hasFrequencyColumn ? "d.frequency" : "NULL::text AS frequency";
        var sql = $$"""
            {{BaseDonationProjection()}}
            WHERE d.supporter_id = @supporter_id
            ORDER BY d.donation_date DESC NULLS LAST, d.donation_id DESC, i.item_id
            """;
        sql = sql.Replace("d.frequency,", $"{frequencyProjection},", StringComparison.Ordinal);

        return (sql, new Dictionary<string, object> { ["supporter_id"] = supporterId });
    }

    private static async Task<bool> ColumnExistsAsync(
        NpgsqlConnection conn,
        string schema,
        string table,
        string column,
        CancellationToken ct)
    {
        const string sql = """
            SELECT EXISTS (
              SELECT 1
              FROM information_schema.columns
              WHERE table_schema = @schema AND table_name = @table AND column_name = @column
            )
            """;

        return await ExecuteScalarAsync<bool>(
            conn,
            sql,
            new Dictionary<string, object>
            {
                ["schema"] = schema,
                ["table"] = table,
                ["column"] = column
            },
            ct);
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

    private sealed record DynamicInsertPlan(
        string Sql,
        IReadOnlyDictionary<string, object> Parameters);

    private sealed record ColumnSchema(
        string Name,
        string DataType,
        bool IsNullable,
        bool HasDefault,
        bool IsIdentity)
    {
        public bool IsRequired => !IsNullable && !HasDefault && !IsIdentity;
    }

    private readonly struct ColumnValueResolution(bool hasValue, object? value)
    {
        public bool HasValue { get; } = hasValue;
        public object? Value { get; } = value;

        public static ColumnValueResolution WithValue(object? value) => new(true, value);
        public static ColumnValueResolution WithNoValue() => new(false, null);
    }

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
