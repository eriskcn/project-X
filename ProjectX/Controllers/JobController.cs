using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectX.Data;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/jobs")]
[Authorize]
public class JobController(ApplicationDbContext context) : ControllerBase
{
    
    // [HttpGet]
    
}