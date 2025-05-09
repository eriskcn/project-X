using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/services")]
public class ServiceController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "RecruiterVerifiedOnly")]
    public async Task<ActionResult<IEnumerable<ServiceResponse>>> GetServices()
    {
        var services = await context.Services.Select(s => new ServiceResponse
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            DayLimit = s.DayLimit,
            Type = s.Type,
            CashPrice = s.CashPrice,
            XTokenPrice = s.XTokenPrice,
            Created = s.Created,
            Modified = s.Modified
        }).ToListAsync();

        return Ok(services);
    }
}