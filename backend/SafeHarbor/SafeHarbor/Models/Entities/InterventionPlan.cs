using SafeHarbor.Models.Lookups;

namespace SafeHarbor.Models.Entities;

public class InterventionPlan : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid ResidentCaseId { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public int StatusStateId { get; set; }
    public string PlanDetails { get; set; } = string.Empty;

    public ResidentCase? ResidentCase { get; set; }
    public StatusState? StatusState { get; set; }
}
