using SafeHarbor.Models.Entities;

namespace SafeHarbor.Models.Lookups;

public class CaseSubcategory : AuditableEntity
{
    public int Id { get; set; }
    public int CaseCategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public CaseCategory? CaseCategory { get; set; }
}
