namespace SafeHarbor.Models.Entities;

public class UserProfile : AuditableEntity
{
    public Guid Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
