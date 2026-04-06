using SafeHarbor.Models.Lookups;

namespace SafeHarbor.Models.Entities;

public class CaseConference : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid ResidentCaseId { get; set; }
    public DateTimeOffset ConferenceDate { get; set; }
    public int StatusStateId { get; set; }
    public string OutcomeSummary { get; set; } = string.Empty;

    public ResidentCase? ResidentCase { get; set; }
    public StatusState? StatusState { get; set; }
}
