using SafeHarbor.Models.Lookups;

namespace SafeHarbor.Models.Entities;

public class ResidentAssessment : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid ResidentCaseId { get; set; }
    public DateTimeOffset AssessedAt { get; set; }
    public int StatusStateId { get; set; }
    public string Notes { get; set; } = string.Empty;

    public ResidentCase? ResidentCase { get; set; }
    public StatusState? StatusState { get; set; }
}
