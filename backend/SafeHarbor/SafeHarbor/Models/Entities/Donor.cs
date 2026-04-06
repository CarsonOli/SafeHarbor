namespace SafeHarbor.Models.Entities;

public class Donor : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTimeOffset LastActivityAt { get; set; }

    public ICollection<Contribution> Contributions { get; set; } = new List<Contribution>();
}
