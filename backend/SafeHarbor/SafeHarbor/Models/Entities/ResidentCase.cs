using SafeHarbor.Models.Lookups;

namespace SafeHarbor.Models.Entities;

public class ResidentCase : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid SafehouseId { get; set; }
    public Guid? ResidentUserId { get; set; }
    public int CaseCategoryId { get; set; }
    public int? CaseSubcategoryId { get; set; }
    public int StatusStateId { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }

    public Safehouse? Safehouse { get; set; }
    public User? ResidentUser { get; set; }
    public CaseCategory? CaseCategory { get; set; }
    public CaseSubcategory? CaseSubcategory { get; set; }
    public StatusState? StatusState { get; set; }

    public ICollection<ResidentAssessment> Assessments { get; set; } = new List<ResidentAssessment>();
    public ICollection<ProcessRecording> ProcessRecordings { get; set; } = new List<ProcessRecording>();
    public ICollection<HomeVisit> HomeVisits { get; set; } = new List<HomeVisit>();
    public ICollection<CaseConference> CaseConferences { get; set; } = new List<CaseConference>();
    public ICollection<InterventionPlan> InterventionPlans { get; set; } = new List<InterventionPlan>();
}
