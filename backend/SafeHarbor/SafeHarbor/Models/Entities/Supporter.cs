namespace SafeHarbor.Models.Entities;

public class Supporter : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty; // Added
    public string Email { get; set; } = string.Empty;
    public decimal LifetimeDonations { get; set; } = 0;    // Added
    public string? PaymentToken { get; set; }              // Added
    public DateTimeOffset LastActivityAt { get; set; } = DateTimeOffset.UtcNow;
    
    // AuditableEntity likely handles CreatedAtUtc/UpdatedAtUtc, 
    // but if you get errors, you can add them manually here:
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Contribution> Contributions { get; set; } = new List<Contribution>();
}
