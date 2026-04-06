namespace SafeHarbor.Models.Entities;

public class SocialPostMetric : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public DateTimeOffset MetricDate { get; set; }
    public int Reach { get; set; }
    public int Engagements { get; set; }
    public int Clicks { get; set; }

    public Campaign? Campaign { get; set; }
}
