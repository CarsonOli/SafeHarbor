using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using SafeHarbor.DTOs;

namespace SafeHarbor.Controllers.Admin;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Policy = PolicyNames.StaffOrAdmin)]
public sealed class AdminDashboardController : ControllerBase
{
    [HttpGet]
    public ActionResult GetSummary()
    {
        // NOTE: The endpoint intentionally returns a versioned "not implemented" envelope
        // instead of placeholder zero values so clients can distinguish "missing feature"
        // from genuine empty operational data.
        return StatusCode(StatusCodes.Status501NotImplemented, new NotImplementedEnvelope(
            ErrorCode: "NotImplemented.v1",
            Message: "Admin dashboard summary is not implemented yet.",
            TraceId: HttpContext.TraceIdentifier,
            ApiVersion: "v1"));
    }
}
