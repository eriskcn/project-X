using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Models;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/locations")]
[Authorize(Policy = "EmailConfirmed")]
public class LocationController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<LocationResponse>>> GetLocations(
        [FromQuery] Region? region,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10
    )
    {
        if (page <= 0 || pageSize < 0)
        {
            return BadRequest(new
                { Message = "Page number must be greater than zero, and page size must be zero or greater." });
        }

        var query = context.Locations.AsQueryable();

        if (region.HasValue)
        {
            query = query.Where(location => location.Region == region.Value);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(location => location.Name.Contains(search));
        }

        var totalItems = await query.CountAsync();

        if (pageSize == 0)
        {
            var locations = await query
                .Select(location => new LocationResponse
                {
                    Id = location.Id,
                    Name = location.Name,
                    Region = location.Region
                })
                .ToListAsync();

            return Ok(new
            {
                Items = locations,
                TotalItems = totalItems,
                TotalPages = 1,
                First = true,
                Last = true,
                PageNumber = 1,
                PageSize = totalItems
            });
        }

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var locationsWithPagination = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(location => new LocationResponse
            {
                Id = location.Id,
                Name = location.Name,
                Region = location.Region
            })
            .ToListAsync();

        return Ok(new
        {
            Items = locationsWithPagination,
            TotalItems = totalItems,
            TotalPages = totalPages,
            First = page == 1,
            Last = page == totalPages,
            PageNumber = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LocationResponse>> GetLocation(Guid id)
    {
        var location = await context.Locations.FindAsync(id);

        if (location == null)
        {
            return NotFound(new { Message = $"Location with id {id} not found" });
        }

        return Ok(new LocationResponse
        {
            Id = location.Id,
            Name = location.Name,
            Region = location.Region
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateLocation([FromBody] LocationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var location = new Location
        {
            Name = request.Name,
            Region = request.Region
        };

        context.Locations.Add(location);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetLocation), new { id = location.Id },
            new { Message = "Create location successfully" });
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateLocation(
        Guid id, [FromBody] UpdateLocationRequest request)
    {
        if (request.Name == null && request.Region == null)
        {
            return BadRequest("At least one field must be provided for update.");
        }

        var location = await context.Locations.FindAsync(id);

        if (location == null)
        {
            return NotFound(new { Message = $"Location with id {id} not found" });
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            location.Name = request.Name;
        }

        if (request.Region.HasValue)
        {
            location.Region = request.Region.Value;
        }

        location.Modified = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(new { Message = "Update location successfully" });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteLocation(Guid id)
    {
        var location = await context.Locations.FindAsync(id);

        if (location == null)
        {
            return NotFound(new { Message = $"Location with id {id} not found" });
        }

        context.Locations.Remove(location);
        await context.SaveChangesAsync();

        return Ok(new { Message = "Delete location successfully" });
    }

    [HttpGet("deleted")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<LocationResponse>>> GetDeletedLocation(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new { Message = "Page number and page size must be greater than zero." });
        }

        var query = context.Locations.IgnoreSoftDelete()
            .Where(location => location.IsDeleted);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(location => location.Name.Contains(search));
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var locations = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(location => new LocationResponse
            {
                Id = location.Id,
                Name = location.Name,
                Region = location.Region
            })
            .ToListAsync();

        var response = new
        {
            Items = locations,
            TotalItems = totalItems,
            TotalPages = totalPages,
            First = page == 1,
            Last = page == totalPages,
            PageNumber = page,
            PageSize = pageSize
        };

        return Ok(response);
    }

    [HttpPatch("restore/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LocationResponse>> RestoreLocation(Guid id)
    {
        var location = await context.Locations.IgnoreSoftDelete()
            .SingleOrDefaultAsync(location => location.Id == id);

        if (location == null)
        {
            return NotFound(new { Message = $"Location with id {id} not found" });
        }

        location.IsDeleted = false;
        await context.SaveChangesAsync();

        return Ok(new LocationResponse
        {
            Id = location.Id,
            Name = location.Name,
            Region = location.Region
        });
    }
}