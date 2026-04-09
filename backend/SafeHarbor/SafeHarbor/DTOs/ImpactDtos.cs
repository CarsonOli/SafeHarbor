namespace SafeHarbor.DTOs;

public sealed record ImpactMetricDto(
    string Label,
    int Value,
    decimal ChangePercent);

public sealed record MonthlyTrendPointDto(
    string Month,
    int AssistedHouseholds);

public sealed record OutcomeDistributionDto(
    string Category,
    int Count);

public sealed record ImpactSummaryDto(
    DateTimeOffset GeneratedAt,
    IReadOnlyCollection<ImpactMetricDto> Metrics,
    IReadOnlyCollection<MonthlyTrendPointDto> MonthlyTrend,
    IReadOnlyCollection<OutcomeDistributionDto> Outcomes);
