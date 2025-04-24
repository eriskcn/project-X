using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/health-check")]
public class HealthCheckController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            Message = "Server is warm",
            Signature = "hoaideeptryvcl",
            Bingo = "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
        });
    }

    [HttpGet("secret")]
    [Authorize]
    [Authorize(Policy = "EmailConfirmed")]
    public IActionResult GetSecret()
    {
        return Ok(new
        {
            Message = "Authorized",
            Bingo = "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
        });
    }
}