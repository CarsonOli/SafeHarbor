namespace SafeHarbor.Models.Entities;

public class UserRole
{
    public Guid Id { get; set; } = Guid.NewGuid(); // Add this line!

    public Guid UserProfileId { get; set; }
    public Guid RoleId { get; set; }

    public UserProfile? UserProfile { get; set; }
    public Role? Role { get; set; }
}