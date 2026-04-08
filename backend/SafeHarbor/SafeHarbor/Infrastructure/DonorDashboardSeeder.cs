using SafeHarbor.Models.Entities;

namespace SafeHarbor.Infrastructure;

/// <summary>
/// Populates the InMemoryDataStore with realistic test data for the donor dashboard.
///
/// HOW TO USE:
///   Call DonorDashboardSeeder.Seed(store) in Program.cs after building the app.
///   The method is idempotent — safe to call multiple times; it exits early if data already exists.
///
/// TEST CREDENTIALS (use on the /login page with role "Donor"):
///   alice@example.com  — primary test donor with 12 months of contribution history
///   bob@example.com    — secondary test donor with a few contributions
///
/// TODO: Remove this seeder once a real database with migration seeds is in place.
/// </summary>
public static class DonorDashboardSeeder
{
    // Hard-coded GUIDs ensure stable IDs across restarts, making testing predictable.
    private static readonly Guid AliceId = Guid.Parse("00000000-0001-0000-0000-000000000001");
    private static readonly Guid BobId   = Guid.Parse("00000000-0001-0000-0000-000000000002");
    private static readonly Guid CampaignId = Guid.Parse("00000000-0003-0000-0000-000000000001");

    // StatusStateId = 1 means "Active" / "Completed" depending on the domain context.
    // These integer keys match the seeded lookup data.
    // TODO: Replace with named constants from a lookup table once the DB is wired.
    private const int CompletedContributionStatusId = 1;
    private const int ActiveCampaignStatusId = 1;
    private const int OnlineDonationTypeId = 1;

    /// <summary>
    /// Seeds donors, a campaign, and contribution history into the store.
    /// Safe to call multiple times — exits immediately if donors already exist.
    /// </summary>
    public static void Seed(InMemoryDataStore store)
    {
        // Guard: don't double-seed if something already populated the store.
        if (store.Donors.Count > 0)
            return;

        // ── Donors ────────────────────────────────────────────────────────────
        // Primary test donor — use alice@example.com with role "Donor" to test the dashboard.
        var alice = new Donor
        {
            Id = AliceId,
            DisplayName = "Alice Nguyen",
            Email = "alice@example.com",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };

        // Secondary test donor — use bob@example.com to verify campaign totals reflect multiple donors.
        var bob = new Donor
        {
            Id = BobId,
            DisplayName = "Bob Chen",
            Email = "bob@example.com",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };

        store.Donors.Add(alice);
        store.Donors.Add(bob);

        // ── Campaign ──────────────────────────────────────────────────────────
        // One active campaign with a $50,000 goal. The donor dashboard shows progress
        // toward this goal using the sum of all contributions linked to this campaign.
        var campaign = new Campaign
        {
            Id = CampaignId,
            Name = "Spring 2026 Safe Homes Drive",
            GoalAmount = 50_000m,
            StartDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            EndDate   = new DateTimeOffset(2026, 6, 30, 0, 0, 0, TimeSpan.Zero),
            StatusStateId = ActiveCampaignStatusId,
        };

        store.Campaigns.Add(campaign);

        // ── Alice's contributions (14 total, Apr 2025 – Mar 2026) ─────────────
        // Spread across 12 months to populate the donation history bar chart.
        // Two months have two contributions to demonstrate the monthly-aggregation logic.
        var now = DateTimeOffset.UtcNow;
        var aliceContributions = new List<Contribution>
        {
            MakeContribution(AliceId, CampaignId,  100m, MonthsAgo(now, 11)), // Apr 2025
            MakeContribution(AliceId, CampaignId,   50m, MonthsAgo(now, 10)), // May 2025
            MakeContribution(AliceId, CampaignId,  200m, MonthsAgo(now,  9)), // Jun 2025
            MakeContribution(AliceId, CampaignId,   75m, MonthsAgo(now,  8)), // Jul 2025
            MakeContribution(AliceId, CampaignId,  150m, MonthsAgo(now,  7)), // Aug 2025
            MakeContribution(AliceId, CampaignId,  250m, MonthsAgo(now,  6)), // Sep 2025
            MakeContribution(AliceId, CampaignId,  100m, MonthsAgo(now,  5)), // Oct 2025
            MakeContribution(AliceId, CampaignId,   50m, MonthsAgo(now,  5)), // Oct 2025 (second donation same month)
            MakeContribution(AliceId, CampaignId,  300m, MonthsAgo(now,  4)), // Nov 2025
            MakeContribution(AliceId, CampaignId,  500m, MonthsAgo(now,  3)), // Dec 2025
            MakeContribution(AliceId, CampaignId,  200m, MonthsAgo(now,  2)), // Jan 2026
            MakeContribution(AliceId, CampaignId,  175m, MonthsAgo(now,  2)), // Jan 2026 (second donation)
            MakeContribution(AliceId, CampaignId,  150m, MonthsAgo(now,  1)), // Feb 2026
            MakeContribution(AliceId, CampaignId,  250m, MonthsAgo(now,  0)), // Mar 2026
        };
        // Alice's lifetime total: 2,550.00

        // ── Bob's contributions (3 total, recent months) ──────────────────────
        // Smaller amounts from a second donor so the campaign total is visibly higher
        // than Alice's individual contribution — demonstrating multi-donor aggregation.
        var bobContributions = new List<Contribution>
        {
            MakeContribution(BobId, CampaignId, 500m, MonthsAgo(now, 2)),
            MakeContribution(BobId, CampaignId, 250m, MonthsAgo(now, 1)),
            MakeContribution(BobId, CampaignId, 100m, MonthsAgo(now, 0)),
        };
        // Bob's contributions to campaign: 850.00
        // Total campaign raised so far: 2,550 + 850 = 3,400 (~6.8% of $50k goal)

        store.Contributions.AddRange(aliceContributions);
        store.Contributions.AddRange(bobContributions);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a single completed contribution linked to a donor and campaign.
    /// All seeded contributions are marked as completed (StatusStateId = 1).
    /// </summary>
    private static Contribution MakeContribution(
        Guid donorId,
        Guid campaignId,
        decimal amount,
        DateTimeOffset date) => new()
    {
        Id = Guid.NewGuid(),
        DonorId = donorId,
        CampaignId = campaignId,
        Amount = amount,
        ContributionDate = date,
        ContributionTypeId = OnlineDonationTypeId,
        StatusStateId = CompletedContributionStatusId,
    };

    /// <summary>Returns a DateTimeOffset approximately N calendar months before the reference date.</summary>
    private static DateTimeOffset MonthsAgo(DateTimeOffset reference, int months)
        => reference.AddMonths(-months);
}
