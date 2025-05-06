using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.Models;

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

public class ServiceResponse
{
    public Guid Id { get; set; }
    [StringLength(150)] public required string Name { get; set; }
    [StringLength(500)] public required string Description { get; set; }
    public required int DayLimit { get; set; }
    public ServiceType Type { get; set; }
    public double CashPrice { get; set; }
    public int XTokenPrice { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}