using System.ComponentModel.DataAnnotations;

namespace SafeHarbor.DTOs;

public sealed record DonorCreateRequest(
    [property: Required, StringLength(120, MinimumLength = 2)] string DisplayName,
    [property: Required, EmailAddress] string Email,
    [property: Range(typeof(decimal), "0", "1000000000")] decimal LifetimeDonations,
    [property: Required, StringLength(256, MinimumLength = 16)] string PaymentToken);

public sealed record DonorUpdateRequest(
    [property: Required, StringLength(120, MinimumLength = 2)] string DisplayName,
    [property: Required, EmailAddress] string Email,
    [property: Range(typeof(decimal), "0", "1000000000")] decimal LifetimeDonations,
    [property: Required, StringLength(256, MinimumLength = 16)] string PaymentToken);

public sealed record DonorAdminResponse(
    Guid Id,
    string DisplayName,
    string Email,
    decimal LifetimeDonations,
    string PaymentToken,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    // ML risk fields — appended, nullable, populated by DonorAdminService.GetAllAsync
    int ContributionCount = 0,
    DateTimeOffset? LastContributionDate = null,
    DateTimeOffset? FirstContributionDate = null,
    int? UniqueChannels = null);

public sealed record DonorPublicResponse(
    Guid Id,
    string DisplayName,
    decimal LifetimeDonations);
