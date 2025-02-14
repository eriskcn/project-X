using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Models;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/locations")]
public class LocationController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LocationResponse>>> GetLocations([FromQuery] Region? region)
    {
        var query = context.Locations.AsQueryable();

        if (region.HasValue)
        {
            query = query.Where(location => location.Region == region.Value);
        }

        var locations = await query
            .Select(location => new LocationResponse
            {
                Id = location.Id,
                Name = location.Name,
                Region = location.Region
            })
            .ToListAsync();

        return Ok(locations);
    }
    [HttpPost]
    public async Task<IActionResult> CreateLocation([FromBody] LocationRequest request)
    {
        var location = new Location
        {
            Name = request.Name,
            Region = request.Region,
            Modified = DateTime.UtcNow
        };

        context.Locations.Add(location);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetLocations), new { id = location.Id }, location);
    }
}