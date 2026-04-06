using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeHarbor.Data;
using SafeHarbor.DTOs;
using SafeHarbor.Models.Entities;

namespace SafeHarbor.Controllers.Admin;

[ApiController]
[Route("api/admin/donors-contributions")]
[Authorize]
public sealed class DonorsContributionsController(SafeHarborDbContext dbContext) : ControllerBase
{
    [HttpGet("donors")]
    public async Task<ActionResult<PagedResult<DonorListItem>>> GetDonors([FromQuery] PagingQuery query, CancellationToken cancellationToken)
    {
        var donorsQuery = dbContext.Donors.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            donorsQuery = donorsQuery.Where(x => x.Name.Contains(query.Search) || x.Email.Contains(query.Search));
        }

        donorsQuery = (query.SortBy?.ToLowerInvariant(), query.Desc) switch
        {
            ("name", true) => donorsQuery.OrderByDescending(x => x.Name),
            ("name", false) => donorsQuery.OrderBy(x => x.Name),
            ("email", true) => donorsQuery.OrderByDescending(x => x.Email),
            ("email", false) => donorsQuery.OrderBy(x => x.Email),
            (_, true) => donorsQuery.OrderByDescending(x => x.LastActivityAt),
            _ => donorsQuery.OrderBy(x => x.LastActivityAt)
        };

        var totalCount = await donorsQuery.CountAsync(cancellationToken);
        var page = query.NormalizedPage;
        var pageSize = query.NormalizedPageSize;

        var items = await donorsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new DonorListItem(
                x.Id,
                x.Name,
                x.Email,
                x.LastActivityAt,
                x.Contributions.Where(c => c.DeletedAt == null).Sum(c => c.Amount)))
            .ToArrayAsync(cancellationToken);

        return Ok(new PagedResult<DonorListItem>(items, page, pageSize, totalCount));
    }

    [HttpPost("donors")]
    public async Task<ActionResult<DonorListItem>> CreateDonor([FromBody] CreateDonorRequest request, CancellationToken cancellationToken)
    {
        var donor = new Donor
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            LastActivityAt = DateTimeOffset.UtcNow
        };

        dbContext.Donors.Add(donor);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetDonors), new { donor.Id }, new DonorListItem(donor.Id, donor.Name, donor.Email, donor.LastActivityAt, 0));
    }

    [HttpPost("contributions")]
    public async Task<ActionResult<ContributionListItem>> LogContribution([FromBody] CreateContributionRequest request, CancellationToken cancellationToken)
    {
        var contribution = new Contribution
        {
            Id = Guid.NewGuid(),
            DonorId = request.DonorId,
            Amount = request.Amount,
            ContributionTypeId = request.ContributionTypeId,
            StatusStateId = request.StatusStateId,
            ContributionDate = request.ContributionDate ?? DateTimeOffset.UtcNow,
            CampaignId = request.CampaignId
        };

        dbContext.Contributions.Add(contribution);

        var donor = await dbContext.Donors.FirstOrDefaultAsync(x => x.Id == request.DonorId, cancellationToken) ?? throw new KeyNotFoundException("Donor not found.");
        donor.LastActivityAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        var statusName = await dbContext.StatusStates.Where(x => x.Id == contribution.StatusStateId).Select(x => x.Name).FirstOrDefaultAsync(cancellationToken) ?? "Unknown";

        return CreatedAtAction(nameof(GetDonors), new { donor.Id }, new ContributionListItem(contribution.Id, donor.Name, contribution.Amount, contribution.ContributionDate, statusName));
    }

    [HttpPost("allocations")]
    public async Task<ActionResult> TrackAllocation([FromBody] CreateAllocationRequest request, CancellationToken cancellationToken)
    {
        var allocation = new ContributionAllocation
        {
            Id = Guid.NewGuid(),
            ContributionId = request.ContributionId,
            SafehouseId = request.SafehouseId,
            AmountAllocated = request.AmountAllocated
        };

        dbContext.ContributionAllocations.Add(allocation);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok();
    }
}
