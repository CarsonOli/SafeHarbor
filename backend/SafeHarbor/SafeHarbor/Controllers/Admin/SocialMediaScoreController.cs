using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using SafeHarbor.DTOs;

namespace SafeHarbor.Controllers.Admin;

/// <summary>
/// Proxies social media post scoring requests to the Python ML sidecar.
/// The XGBoost model (AUC 0.898) lives in ml-service/ and is served by FastAPI.
/// This controller adds authentication and translates the typed C# DTO to JSON.
///
/// Environment variable: MlService__BaseUrl
/// Local default:        http://localhost:8000
/// Azure:                set in App Service / Container App configuration
/// </summary>
[ApiController]
[Route("api/admin/social-media")]
[Authorize(Policy = PolicyNames.AdminOnly)]
public sealed class SocialMediaScoreController(
    IHttpClientFactory httpFactory,
    IConfiguration     config,
    ILogger<SocialMediaScoreController> logger) : ControllerBase
{
    [HttpPost("score-post")]
    public async Task<ActionResult<SocialMediaScoreResponse>> ScorePost(
        [FromBody] SocialMediaScoreRequest request,
        CancellationToken ct)
    {
        var mlServiceUrl = config["MlService:BaseUrl"] ?? "http://localhost:8000";

        try
        {
            var client  = httpFactory.CreateClient("MlService");
            var json    = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
            var content  = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{mlServiceUrl}/score-post", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("ML service returned {StatusCode}", response.StatusCode);
                return StatusCode(502, "ML service unavailable");
            }

            var body   = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<SocialMediaScoreResponse>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result is null ? StatusCode(502, "Invalid ML service response") : Ok(result);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to reach ML service at {Url}", mlServiceUrl);
            return StatusCode(502, "ML service unreachable");
        }
    }
}
