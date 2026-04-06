namespace SafeHarbor.Models.Entities;

public class OutcomeSnapshot : AuditableEntity
{
    public Guid Id { get; set; }
    public DateOnly SnapshotDate { get; set; }
    public int TotalResidentsServed { get; set; }
    public int TotalHomeVisits { get; set; }
    public decimal TotalContributions { get; set; }
    public decimal CampaignEngagementRate { get; set; }
    public string Notes { get; set; } = string.Empty;
}
