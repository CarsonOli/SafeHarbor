namespace SafeHarbor.Models.Entities;

public class User
{
    public Guid UserId { get; set; }
    // Nullable by design: staff/admin users may not map to a donor/supporter CRM profile.
    public long? SupporterId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
