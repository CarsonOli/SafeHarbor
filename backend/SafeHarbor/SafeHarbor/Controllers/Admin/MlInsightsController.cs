using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using SafeHarbor.DTOs;
using SafeHarbor.Services;

namespace SafeHarbor.Controllers.Admin;

/// <summary>
/// ML insights endpoints — donor risk flags and resident readiness flags.
/// Both are rule-based systems derived from explanatory logistic regression
/// coefficient analysis (see ml-pipelines/ notebooks for full methodology).
/// </summary>
[ApiController]
[Route("api/admin/ml")]
[Authorize(Policy = PolicyNames.AdminOnly)]
public sealed class MlInsightsController(
    IDonorAdminService            donorService,
    IDonorRiskFlagService         riskFlagService,
    IResidentAdminService         residentService,
    IResidentReadinessFlagService readinessService) : ControllerBase
{
    /// <summary>
    /// Returns a lapse risk flag for every donor.
    /// Flags are computed server-side from donation history; no ML model file required.
    /// </summary>
    [HttpGet("donor-risk-flags")]
    public async Task<ActionResult<IReadOnlyList<DonorRiskFlagResponse>>> GetDonorRiskFlags(
        CancellationToken ct)
    {
        var donors = await donorService.GetAllAsync(ct);

        var results = donors.Select(d =>
        {
            var now       = DateTime.UtcNow;
            var lastDate  = d.LastContributionDate?.UtcDateTime ?? now;
            var firstDate = d.FirstContributionDate?.UtcDateTime ?? now;

            var daysSince = (now - lastDate).TotalDays;
            var totalDays = Math.Max(1, (now - firstDate).TotalDays);
            var freq      = d.ContributionCount / totalDays;
            var avgGap    = d.ContributionCount > 1
                              ? totalDays / (double)(d.ContributionCount - 1)
                              : 999;

            var flag = riskFlagService.ComputeFlag(new DonorRiskInput(
                daysSince,
                freq,
                d.ContributionCount,
                d.UniqueChannels ?? 1,
                avgGap));

            return new DonorRiskFlagResponse(
                d.Id.ToString(),
                d.DisplayName ?? "Unknown",
                flag.Level,
                flag.Score,
                flag.Signals);
        }).ToList();

        return Ok(results);
    }

    /// <summary>
    /// Returns a reintegration readiness flag for every active resident.
    /// Flags are computed server-side from resident data; no ML model file required.
    /// </summary>
    [HttpGet("resident-readiness-flags")]
    public async Task<ActionResult<IReadOnlyList<ResidentReadinessFlagResponse>>> GetResidentReadinessFlags(
        CancellationToken ct)
    {
        var residents = await residentService.GetAllAsync(ct);

        var results = residents.Select(r =>
        {
            var flag = readinessService.ComputeFlag(new ResidentReadinessInput(
                TotalVisits:          r.TotalVisits ?? 0,
                AvgFamilyCooperation: r.AvgFamilyCooperation ?? 2.0,
                PctPsychDone:         r.PctPsychCheckupsDone ?? 0.0,
                RiskImprovement:      r.RiskImprovement ?? 0,
                AvgProgressPct:       r.AvgProgressPct ?? 0.0,
                FamilySoloParent:     r.FamilySoloParent,
                CaseCategory:         r.CaseCategory ?? "",
                PctSafetyConcerns:    r.PctSafetyConcerns ?? 0.0));

            return new ResidentReadinessFlagResponse(
                r.Id.ToString(),
                flag.Level,
                flag.Score,
                flag.Action,
                flag.Signals);
        }).ToList();

        return Ok(results);
    }
}
