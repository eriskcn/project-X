using Microsoft.AspNetCore.Mvc;
using ProjectX.Data;

namespace ProjectX.Controllers;
[ApiController]
[Route("capablanca/api/v0/business")]
public class BusinessController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    public BusinessController(ApplicationDbContext context)
    {
        _context = context;
    }

    
}