namespace SafeHarbor.Models;

public sealed class Donor
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal LifetimeDonations { get; set; }
    public string PaymentToken { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
