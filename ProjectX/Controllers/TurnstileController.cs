using Microsoft.AspNetCore.Mvc;
using ProjectX.DTOs.Turnstiles;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/turnstile")]
public class TurnstileController(IHttpClientFactory httpClientFactory, IConfiguration configuration) : ControllerBase
{
    private readonly string _secretKey = configuration["Cloudflare:Turnstile:SecretKey"] ?? throw new
        InvalidOperationException();

    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] TurnstileVerifyRequest request)
    {
        if (string.IsNullOrEmpty(request.Token))
        {
            return BadRequest(new { success = false, message = "Token is required" });
        }

        var client = httpClientFactory.CreateClient();
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("secret", _secretKey),
            new KeyValuePair<string, string>("response", request.Token),
            new KeyValuePair<string, string>("remoteip", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "")
        });

        var response = await client.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", content);
        var result = await response.Content.ReadFromJsonAsync<TurnstileVerifyResponse>();

        if (result?.Success == true)
        {
            return Ok(new { success = true });
        }

        return BadRequest(new
        {
            success = false,
            message = result?.ErrorCodes?.FirstOrDefault() ?? "Verification failed"
        });
    }
}