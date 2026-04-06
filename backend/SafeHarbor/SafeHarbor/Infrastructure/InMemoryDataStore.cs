using SafeHarbor.Models;

namespace SafeHarbor.Infrastructure;

public sealed class InMemoryDataStore
{
    public List<Resident> Residents { get; } =
    [
        new Resident
        {
            FullName = "Alex Monroe",
            DateOfBirth = new DateOnly(1994, 11, 22),
            MedicalNotes = "Asthma inhaler required.",
            CaseWorkerEmail = "caseworker@safeharbor.org"
        }
    ];

    public List<Donor> Donors { get; } =
    [
        new Donor
        {
            DisplayName = "Harbor Foundation",
            Email = "finance@harborfoundation.org",
            LifetimeDonations = 75_000m,
            PaymentToken = "tok_live_demo_redacted_0001"
        }
    ];
}
