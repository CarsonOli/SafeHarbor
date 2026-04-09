using SafeHarbor.DTOs;

namespace SafeHarbor.Services.Public;

public interface IImpactAggregateService
{
    Task<ImpactSummaryDto> GetAggregateAsync(CancellationToken ct);
}
