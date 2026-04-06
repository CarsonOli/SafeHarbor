namespace SafeHarbor.Models;

public sealed class Resident
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string MedicalNotes { get; set; } = string.Empty;
    public string CaseWorkerEmail { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
