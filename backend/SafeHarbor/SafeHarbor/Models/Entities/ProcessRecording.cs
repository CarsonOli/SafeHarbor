namespace SafeHarbor.Models.Entities;

public class ProcessRecording : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid ResidentCaseId { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
    public string SocialWorker { get; set; } = string.Empty;
    public string SessionType { get; set; } = "Individual";
    public int? SessionDurationMinutes { get; set; }
    public string EmotionalStateObserved { get; set; } = string.Empty;
    public string? EmotionalStateEnd { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? InterventionsApplied { get; set; }
    public string? FollowUpActions { get; set; }
    public bool ProgressNoted { get; set; }
    public bool ConcernsFlagged { get; set; }
    public bool ReferralMade { get; set; }
    public string? NotesRestricted { get; set; }

    public ResidentCase? ResidentCase { get; set; }
}
