using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SafeHarbor.Data;
using SafeHarbor.Models.Entities;

namespace SafeHarbor.Tests;

public sealed class SupporterPersistenceMappingTests
{
    [Fact]
    public void DbContext_MapsSupporterPersistence_ToSupportersTable()
    {
        var options = new DbContextOptionsBuilder<SafeHarborDbContext>()
            .UseInMemoryDatabase($"mapping-tests-{Guid.NewGuid()}")
            .Options;

        using var db = new SafeHarborDbContext(options);

        var supporterEntity = db.Model.FindEntityType(typeof(Supporter));
        Assert.NotNull(supporterEntity);
        Assert.Equal("supporters", supporterEntity!.GetTableName());
        Assert.Equal("lighthouse", supporterEntity.GetSchema());

        var contributionEntity = db.Model.FindEntityType(typeof(Contribution));
        Assert.NotNull(contributionEntity);

        var supporterIdProperty = contributionEntity!.FindProperty(nameof(Contribution.SupporterId));
        Assert.NotNull(supporterIdProperty);
        Assert.Equal("supporter_id", supporterIdProperty!.GetColumnName());
        Assert.Null(contributionEntity.FindProperty("DonorId"));
    }
}
