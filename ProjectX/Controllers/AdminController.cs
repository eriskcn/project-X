using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectX.Data;
using ProjectX.Models;

namespace ProjectX.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("capablanca/api/v0/admin")]
public class AdminController(ApplicationDbContext context) : ControllerBase
{
    // public async Task<ActionResult<IEnumerable<CompanyDetail>>> GetCompanies()
    // {
    // }
}