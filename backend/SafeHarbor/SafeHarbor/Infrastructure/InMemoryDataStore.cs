using SafeHarbor.Models;
using SafeHarbor.Models.Entities;

namespace SafeHarbor.Infrastructure;

/// <summary>
/// In-memory data store used during local development and testing.
/// All collections start empty; call DonorDashboardSeeder.Seed() in Program.cs
/// to populate with test data for the donor dashboard.
///
/// TODO: Replace with Entity Framework + PostgreSQL once infrastructure is available.
/// </summary>
public sealed class InMemoryDataStore
{
    // NOTE: This store intentionally starts empty.
    // TODO: Replace InMemoryDataStore with a database-backed repository once infrastructure is available.
    public List<Resident> Residents { get; } = [];

    // NOTE: Keep this collection empty so no synthetic financial records are mistaken for real donor data.
    // Seeding is done explicitly via DonorDashboardSeeder, not here.
    public List<SafeHarbor.Models.Entities.Donor> Donors { get; } = [];

    /// <summary>Fundraising campaigns. Populated by DonorDashboardSeeder at startup.</summary>
    public List<Campaign> Campaigns { get; } = [];

    /// <summary>Individual donation records. Populated by DonorDashboardSeeder and by POST /api/donor/contribution.</summary>
    public List<Contribution> Contributions { get; } = [];
}
