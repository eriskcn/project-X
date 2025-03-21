using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/deeptryvcl")]
public class EasterEggController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
    }
    
    [HttpGet("secret")]
    [Authorize]
    public IActionResult GetSecret()
    {
        return Ok("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
    }
}