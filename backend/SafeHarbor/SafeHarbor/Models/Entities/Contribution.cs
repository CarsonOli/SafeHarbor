using SafeHarbor.Models.Lookups;

namespace SafeHarbor.Models.Entities;

public class Contribution : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid DonorId { get; set; }
    public Guid? CampaignId { get; set; }
    public int ContributionTypeId { get; set; }
    public int StatusStateId { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset ContributionDate { get; set; }

    public Donor? Donor { get; set; }
    public Campaign? Campaign { get; set; }
    public ContributionType? ContributionType { get; set; }
    public StatusState? StatusState { get; set; }

    public ICollection<ContributionAllocation> Allocations { get; set; } = new List<ContributionAllocation>();
}
