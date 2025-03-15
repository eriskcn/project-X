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
}