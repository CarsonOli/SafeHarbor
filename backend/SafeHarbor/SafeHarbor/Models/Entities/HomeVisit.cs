using SafeHarbor.Models.Lookups;

namespace SafeHarbor.Models.Entities;

public class HomeVisit : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid ResidentCaseId { get; set; }
    public int VisitTypeId { get; set; }
    public int StatusStateId { get; set; }
    public DateTimeOffset VisitDate { get; set; }
    public string Notes { get; set; } = string.Empty;

    public ResidentCase? ResidentCase { get; set; }
    public VisitType? VisitType { get; set; }
    public StatusState? StatusState { get; set; }
}
