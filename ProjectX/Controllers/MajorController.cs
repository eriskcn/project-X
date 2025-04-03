using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Models;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/majors")]
[Authorize]
public class MajorController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MajorResponse>>> GetMajors(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page <= 0 || pageSize < 0)
        {
            return BadRequest(new
                { Message = "Page number must be greater than zero, and page size must be zero or greater." });
        }

        var query = context.Majors.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(m => m.Name.Contains(search));
        }

        var totalItems = await query.CountAsync();

        if (pageSize == 0)
        {
            var majors = await query
                .Select(m => new MajorResponse
                {
                    Id = m.Id,
                    Name = m.Name
                })
                .ToListAsync();

            return Ok(new
            {
                Items = majors,
                TotalItems = totalItems,
                TotalPages = 1,
                First = true,
                Last = true,
                PageNumber = 1,
                PageSize = totalItems
            });
        }

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var majorsWithPagination = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MajorResponse
            {
                Id = m.Id,
                Name = m.Name
            })
            .ToListAsync();

        return Ok(new
        {
            Items = majorsWithPagination,
            TotalItems = totalItems,
            TotalPages = totalPages,
            First = page == 1,
            Last = page == totalPages,
            PageNumber = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MajorResponse>> GetMajor(Guid id)
    {
        var major = await context.Majors.FindAsync(id);
        if (major == null)
        {
            return NotFound(new { Message = $"Major with id {id} not found." });
        }

        return Ok(new MajorResponse
        {
            Id = major.Id,
            Name = major.Name
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MajorResponse>> CreateMajor([FromBody] MajorRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var major = new Major
        {
            Id = Guid.NewGuid(),
            Name = request.Name
        };

        context.Majors.Add(major);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMajor), new { id = major.Id }, new MajorResponse
        {
            Id = major.Id,
            Name = major.Name
        });
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<MajorResponse>> UpdateMajor(Guid id, [FromBody] UpdateMajorRequest request)
    {
        var major = await context.Majors.FindAsync(id);
        if (major == null)
        {
            return NotFound(new { Message = $"Major with id {id} not found." });
        }

        major.Name = request.Name;
        await context.SaveChangesAsync();

        return Ok(new MajorResponse
        {
            Id = major.Id,
            Name = major.Name
        });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteMajor(Guid id)
    {
        var major = await context.Majors.FindAsync(id);
        if (major == null)
        {
            return NotFound(new { Message = $"Major with id {id} not found." });
        }

        context.Majors.Remove(major);
        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("deleted")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<MajorResponse>>> GetDeletedMajors(
        [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new { Message = "Page number and page size must be greater than zero." });
        }

        var query = context.Majors.IgnoreSoftDelete()
            .Where(m => m.IsDeleted);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(m => m.Name.Contains(search));
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var majors = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MajorResponse
            {
                Id = m.Id,
                Name = m.Name
            })
            .ToListAsync();

        var response = new
        {
            Items = majors,
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
    public async Task<ActionResult<MajorResponse>> RestoreMajor(Guid id)
    {
        var major = await context.Majors.IgnoreSoftDelete()
            .SingleOrDefaultAsync(m => m.Id == id);

        if (major == null)
        {
            return NotFound(new { Message = $"Major with id: {id} not found." });
        }

        major.IsDeleted = false;
        await context.SaveChangesAsync();

        return Ok(new MajorResponse
        {
            Id = major.Id,
            Name = major.Name
        });
    }
}