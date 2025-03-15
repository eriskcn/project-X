using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectX.Data;

namespace ProjectX.Controllers;
[ApiController]
[Route("capablanca/api/v0/posts")]
[Authorize]
public class PostController(ApplicationDbContext context) : ControllerBase
{
    
}