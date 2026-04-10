using SafeHarbor.DTOs;
using SafeHarbor.Models;
using SafeHarbor.Models.Entities;

namespace SafeHarbor.Services;

// Repository interfaces keep controller/service orchestration decoupled from concrete persistence details.
// We bind these to EF Core for deployed environments and can swap to in-memory only via explicit dev flag.
public interface IResidentRepository
{
    Task<IReadOnlyList<Resident>> ListAsync(CancellationToken ct);
    Task<Resident?> FindAsync(Guid id, CancellationToken ct);
    Task<Resident> CreateAsync(Resident resident, CancellationToken ct);
    Task<Resident?> UpdateAsync(Resident resident, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

public interface IDonorRepository
{
    Task<IReadOnlyList<Supporter>> ListAsync(CancellationToken ct);
    Task<Supporter?> FindAsync(Guid id, CancellationToken ct);
    Task<Supporter?> FindByEmailAsync(string email, CancellationToken ct);
    Task<Supporter> CreateAsync(Supporter supporter, CancellationToken ct);
    Task<Supporter?> UpdateAsync(Supporter supporter, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

public interface ICampaignRepository
{
    Task<IReadOnlyList<Campaign>> ListAsync(CancellationToken ct);
    Task<Campaign?> GetActiveAsync(CancellationToken ct);
}

public interface IContributionRepository
{
    Task<IReadOnlyList<Contribution>> ListCompletedAsync(CancellationToken ct);
    Task<IReadOnlyList<Contribution>> ListCompletedByDonorAsync(Guid supporterId, CancellationToken ct);
    Task<Contribution> AddAsync(Contribution contribution, CancellationToken ct);
}

public interface IResidentAdminService
{
    Task<IReadOnlyCollection<ResidentAdminResponse>> GetAllAsync(CancellationToken ct);
    Task<ResidentAdminResponse?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<ResidentAdminResponse> CreateAsync(ResidentCreateRequest request, string actor, CancellationToken ct);
    Task<ResidentAdminResponse?> UpdateAsync(Guid id, ResidentUpdateRequest request, string actor, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, string actor, CancellationToken ct);
    Task<IReadOnlyCollection<ResidentAdminResponse>> ExportSnapshotAsync(CancellationToken ct);
}

public interface IDonorAdminService
{
    Task<IReadOnlyCollection<DonorAdminResponse>> GetAllAsync(CancellationToken ct);
    Task<DonorAdminResponse?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<DonorAdminResponse> CreateAsync(DonorCreateRequest request, string actor, CancellationToken ct);
    Task<DonorAdminResponse?> UpdateAsync(Guid id, DonorUpdateRequest request, string actor, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, string actor, CancellationToken ct);
    Task<IReadOnlyCollection<DonorPublicResponse>> ReportSummaryAsync(CancellationToken ct);
}

public interface IPublicRecordsService
{
    Task<IReadOnlyCollection<ResidentPublicResponse>> GetResidentsAsync(CancellationToken ct);
    Task<IReadOnlyCollection<DonorPublicResponse>> GetDonorsAsync(CancellationToken ct);
}

public interface IDonorDashboardService
{
    Task<DonorDashboardResponse?> GetDashboardAsync(Guid? supporterId, string? email, CancellationToken ct);
    Task<NewContributionResponse?> AddContributionAsync(Guid? supporterId, string? email, NewContributionRequest request, CancellationToken ct);
}

public interface IDonorAnalyticsService
{
    Task<DonorAnalyticsResponse> GetAnalyticsAsync(CancellationToken ct);
}

