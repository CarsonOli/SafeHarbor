using System.ComponentModel.DataAnnotations;

namespace SafeHarbor.DTOs;

public sealed record DonationFiltersQuery(
    DateTimeOffset? FromDate,
    DateTimeOffset? ToDate,
    string? DonationType,
    string? Campaign,
    string? ChannelSource,
    string? SupporterType,
    string? Frequency,
    int Page = 1,
    int PageSize = 25);

public sealed record DonationListItem(
    long DonationId,
    DateTimeOffset? DonationDate,
    string DonationType,
    decimal Amount,
    decimal EstimatedValue,
    string? CampaignName,
    string? ChannelSource,
    string? Frequency,
    string? Notes,
    long SupporterId,
    string DonorDisplayName,
    string? SupporterType,
    string? SupporterEmail,
    IReadOnlyCollection<InKindDonationItemDto> InKindItems);

public sealed record InKindDonationItemDto(
    long ItemId,
    string? ItemName,
    string? ItemCategory,
    decimal Quantity,
    string? UnitOfMeasure,
    decimal EstimatedUnitValue);

public sealed record YourDonationsResponse(
    bool HasLinkedSupporter,
    long? SupporterId,
    string? SupporterDisplayName,
    IReadOnlyCollection<DonationListItem> Donations);

public sealed record LinkUserToSupporterRequest(
    [property: Required] Guid UserId,
    [property: Range(1, long.MaxValue)] long SupporterId);
