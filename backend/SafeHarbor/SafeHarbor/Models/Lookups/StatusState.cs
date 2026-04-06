using SafeHarbor.Models.Entities;
using SafeHarbor.Models.Enums;

namespace SafeHarbor.Models.Lookups;

public class StatusState : AuditableEntity
{
    public int Id { get; set; }
    public StatusDomain Domain { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
