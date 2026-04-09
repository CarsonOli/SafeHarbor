using System.Net;
using System.Text.Json;

namespace SafeHarbor.Tests;

public sealed class ImpactAggregateIntegrationTests : IClassFixture<SafeHarborApiFactory>
{
    private readonly SafeHarborApiFactory _factory;

    public ImpactAggregateIntegrationTests(SafeHarborApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AggregateImpactEndpoint_AllowsAnonymousAccess()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/impact/aggregate");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AggregateImpactPayload_DoesNotExposePiiFieldsOrKnownSensitiveValues()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/impact/aggregate");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        var forbiddenPropertyNames = new[]
        {
            "residentId",
            "residentCaseId",
            "fullName",
            "name",
            "email",
            "displayName",
            "notes"
        };

        // NOTE: This regression test intentionally inspects raw JSON keys to catch accidental DTO expansion
        // that could leak personally identifiable data from resident or donor tables.
        AssertNoForbiddenProperties(document.RootElement, forbiddenPropertyNames);

        var forbiddenValues = new[]
        {
            "donor1@example.com",
            "Donor One",
            "22222222-2222-2222-2222-222222222222"
        };

        foreach (var forbiddenValue in forbiddenValues)
        {
            Assert.DoesNotContain(forbiddenValue, json, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static void AssertNoForbiddenProperties(JsonElement element, IReadOnlyCollection<string> forbiddenPropertyNames)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    Assert.DoesNotContain(property.Name, forbiddenPropertyNames, StringComparer.OrdinalIgnoreCase);
                    AssertNoForbiddenProperties(property.Value, forbiddenPropertyNames);
                }

                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    AssertNoForbiddenProperties(item, forbiddenPropertyNames);
                }

                break;
        }
    }
}
