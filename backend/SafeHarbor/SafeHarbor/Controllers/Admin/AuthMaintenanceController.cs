using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using SafeHarbor.Services.Auth;

namespace SafeHarbor.Controllers.Admin;

[ApiController]
[Route("api/admin/auth-maintenance")]
[Authorize(Policy = PolicyNames.AdminOnly)]
public sealed class AuthMaintenanceController(IAuthService authService) : ControllerBase
{
    [HttpPost("reconcile-domain-profiles")]
    public async Task<ActionResult<DomainProfileReconciliationResult>> ReconcileDomainProfiles(CancellationToken cancellationToken)
    {
        // NOTE: This explicit endpoint allows operational backfill for pre-existing lighthouse.users rows
        // without requiring direct DB scripts, and keeps idempotent profile provisioning logic centralized.
        var result = await authService.ReconcileDomainProfilesAsync(cancellationToken);
        return Ok(result);
    }
}
