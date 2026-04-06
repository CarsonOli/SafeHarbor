namespace SafeHarbor.Models.Entities;

public class ContributionAllocation : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid ContributionId { get; set; }
    public Guid SafehouseId { get; set; }
    public decimal AmountAllocated { get; set; }

    public Contribution? Contribution { get; set; }
    public Safehouse? Safehouse { get; set; }
}
