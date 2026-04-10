using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeHarbor.Authorization;
using System.Text;
using System.Text.Json;

namespace SafeHarbor.Controllers.Admin;

[ApiController]
[Route("api/admin/social-media")]
[Authorize(Policy = PolicyNames.StaffOrAdmin)]
public sealed class SocialMediaController : ControllerBase
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public SocialMediaController(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    [HttpPost("score-post")]
    public async Task<IActionResult> ScorePost([FromBody] JsonElement body)
    {
        var mlUrl = _config["MlService:BaseUrl"] ?? "http://localhost:5050";
        var client = _http.CreateClient();
        var content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json"
        );

        try
        {
            var response = await client.PostAsync($"{mlUrl}/score-post", content);
            var result = await response.Content.ReadAsStringAsync();
            return Content(result, "application/json");
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(503, new { error = "ML service unavailable", detail = ex.Message });
        }
    }
}