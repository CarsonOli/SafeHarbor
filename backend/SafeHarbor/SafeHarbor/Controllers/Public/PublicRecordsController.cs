using Microsoft.AspNetCore.Mvc;
using SafeHarbor.DTOs;
using SafeHarbor.Infrastructure;

namespace SafeHarbor.Controllers.Public;

[ApiController]
[Route("api/public")]
public sealed class PublicRecordsController(InMemoryDataStore store) : ControllerBase
{
    [HttpGet("residents")]
    public ActionResult<IReadOnlyCollection<ResidentPublicResponse>> GetResidents()
    {
        var residents = store.Residents
            .Select(r => new ResidentPublicResponse(r.Id, r.FullName, CalculateAgeYears(r.DateOfBirth)))
            .ToArray();

        return Ok(residents);
    }

    [HttpGet("donors")]
    public ActionResult<IReadOnlyCollection<DonorPublicResponse>> GetDonors()
    {
        var donors = store.Donors
            .Select(d => new DonorPublicResponse(d.Id, d.DisplayName, d.LifetimeDonations))
            .ToArray();

        return Ok(donors);
    }

    private static int CalculateAgeYears(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var years = today.Year - dateOfBirth.Year;

        if (today < dateOfBirth.AddYears(years))
        {
            years--;
        }

        return years;
    }
}
