using SafeHarbor.Models.Entities;

namespace SafeHarbor.Models.Lookups;

public class ContributionType : AuditableEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
