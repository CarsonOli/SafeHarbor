namespace SafeHarbor.Models.Entities;

public class Safehouse : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;

    public ICollection<ResidentCase> ResidentCases { get; set; } = new List<ResidentCase>();
}
