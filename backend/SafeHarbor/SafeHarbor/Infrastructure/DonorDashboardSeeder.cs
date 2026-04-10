using SafeHarbor.Models.Entities;

namespace SafeHarbor.Infrastructure;

/// <summary>
/// Populates the InMemoryDataStore with realistic test data for the donor dashboard.
/// </summary>
public static class DonorDashboardSeeder
{
    private static readonly Guid AliceId = Guid.Parse("00000000-0001-0000-0000-000000000001");
    private static readonly Guid BobId = Guid.Parse("00000000-0001-0000-0000-000000000002");
    private static readonly Guid CampaignId = Guid.Parse("00000000-0003-0000-0000-000000000001");

    private const int CompletedContributionStatusId = 1;
    private const int ActiveCampaignStatusId = 1;
    private const int OnlineDonationTypeId = 1;

    public static void Seed(InMemoryDataStore store)
    {
        if (store.Supporters.Count > 0)
        {
            return;
        }

        var alice = new Supporter
        {
            Id = AliceId,
            DisplayName = "Alice Nguyen",
            Email = "alice@example.com",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };

        var bob = new Supporter
        {
            Id = BobId,
            DisplayName = "Bob Chen",
            Email = "bob@example.com",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };

        store.Supporters.Add(alice);
        store.Supporters.Add(bob);

        store.Campaigns.Add(new Campaign
        {
            Id = CampaignId,
            Name = "Spring 2026 Safe Homes Drive",
            GoalAmount = 50_000m,
            StartDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2026, 6, 30, 0, 0, 0, TimeSpan.Zero),
            StatusStateId = ActiveCampaignStatusId,
        });

        var now = DateTimeOffset.UtcNow;
        var aliceContributions = new List<Contribution>
        {
            MakeContribution(AliceId, CampaignId, 100m, MonthsAgo(now, 11)),
            MakeContribution(AliceId, CampaignId, 50m, MonthsAgo(now, 10)),
            MakeContribution(AliceId, CampaignId, 200m, MonthsAgo(now, 9)),
            MakeContribution(AliceId, CampaignId, 75m, MonthsAgo(now, 8)),
            MakeContribution(AliceId, CampaignId, 150m, MonthsAgo(now, 7)),
            MakeContribution(AliceId, CampaignId, 250m, MonthsAgo(now, 6)),
            MakeContribution(AliceId, CampaignId, 100m, MonthsAgo(now, 5)),
            MakeContribution(AliceId, CampaignId, 50m, MonthsAgo(now, 5)),
            MakeContribution(AliceId, CampaignId, 300m, MonthsAgo(now, 4)),
            MakeContribution(AliceId, CampaignId, 500m, MonthsAgo(now, 3)),
            MakeContribution(AliceId, CampaignId, 200m, MonthsAgo(now, 2)),
            MakeContribution(AliceId, CampaignId, 175m, MonthsAgo(now, 2)),
            MakeContribution(AliceId, CampaignId, 150m, MonthsAgo(now, 1)),
            MakeContribution(AliceId, CampaignId, 250m, MonthsAgo(now, 0)),
        };

        var bobContributions = new List<Contribution>
        {
            MakeContribution(BobId, CampaignId, 500m, MonthsAgo(now, 2)),
            MakeContribution(BobId, CampaignId, 250m, MonthsAgo(now, 1)),
            MakeContribution(BobId, CampaignId, 100m, MonthsAgo(now, 0)),
        };

        store.Contributions.AddRange(aliceContributions);
        store.Contributions.AddRange(bobContributions);
    }

    private static Contribution MakeContribution(
        Guid supporterId,
        Guid campaignId,
        decimal amount,
        DateTimeOffset date)
        => new()
        {
            Id = Guid.NewGuid(),
            SupporterId = supporterId,
            CampaignId = campaignId,
            Amount = amount,
            ContributionDate = date,
            ContributionTypeId = OnlineDonationTypeId,
            StatusStateId = CompletedContributionStatusId,
        };

    private static DateTimeOffset MonthsAgo(DateTimeOffset reference, int months)
        => reference.AddMonths(-months);
}
