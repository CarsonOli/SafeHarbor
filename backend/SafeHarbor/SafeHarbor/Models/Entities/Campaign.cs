using SafeHarbor.Models.Lookups;

namespace SafeHarbor.Models.Entities;

public class Campaign : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public int StatusStateId { get; set; }

    public StatusState? StatusState { get; set; }
    public ICollection<Contribution> Contributions { get; set; } = new List<Contribution>();
    public ICollection<SocialPostMetric> SocialPostMetrics { get; set; } = new List<SocialPostMetric>();
}
