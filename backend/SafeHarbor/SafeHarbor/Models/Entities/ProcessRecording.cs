namespace SafeHarbor.Models.Entities;

public class ProcessRecording : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid ResidentCaseId { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
    public string Summary { get; set; } = string.Empty;

    public ResidentCase? ResidentCase { get; set; }
}
