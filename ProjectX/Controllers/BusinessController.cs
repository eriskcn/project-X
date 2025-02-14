using Microsoft.AspNetCore.Mvc;
using ProjectX.Data;

namespace ProjectX.Controllers;
[ApiController]
[Route("capablanca/api/v0/business")]
public class BusinessController(ApplicationDbContext context) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;
}