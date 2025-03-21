using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Models;

namespace ProjectX.Controllers;

[Authorize]
[ApiController]
[Route("capablanca/api/v0/skills")]
public class SkillController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SkillResponse>>> GetSkills(
        [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new { Message = "Page number and page size must be greater than zero." });
        }

        var query = context.Skills.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(skill => skill.Name.Contains(search));
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var skills = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var response = new
        {
            Items = skills,
            TotalItems = totalItems,
            TotalPages = totalPages,
            First = page == 1,
            Last = page == totalPages,
            PageNumber = page,
            PageSize = pageSize
        };

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SkillResponse>> GetSkill(Guid id)
    {
        var skill = await context.Skills.FindAsync(id);

        if (skill == null)
        {
            return NotFound(new { Message = "Skill not found." });
        }

        return Ok(new SkillResponse
        {
            Id = skill.Id,
            Name = skill.Name,
            Description = skill.Description
        });
    }

    [HttpPost]
    public async Task<ActionResult<SkillResponse>> CreateSkill([FromBody] SkillRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var skill = new Skill
        {
            Name = request.Name,
            Description = request.Description
        };

        context.Skills.Add(skill);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSkill), new { id = skill.Id },
            new SkillResponse
            {
                Id = skill.Id,
                Name = skill.Name,
                Description = skill.Description
            });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSkill(Guid id)
    {
        var skill = await context.Skills.FindAsync(id);

        if (skill == null)
        {
            return NotFound(new { Message = "Skill not found." });
        }

        context.Skills.Remove(skill);
        await context.SaveChangesAsync();

        return Ok(new { Message = "Skill deleted successfully." });
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SkillResponse>> UpdateSkill(
        Guid id, [FromBody] UpdateSkillRequest request)
    {
        if (request.Name == null && request.Description == null)
        {
            return BadRequest(new { Message = "At least one field must be provided for update." });
        }

        var skill = await context.Skills.FindAsync(id);

        if (skill == null)
        {
            return NotFound(new { Message = "Skill not found." });
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            skill.Name = request.Name;
        }

        if (!string.IsNullOrEmpty(request.Description))
        {
            skill.Description = request.Description;
        }

        skill.Modified = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return Ok(new SkillResponse
        {
            Id = skill.Id,
            Name = skill.Name,
            Description = skill.Description
        });
    }

    [HttpGet("deleted")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<SkillResponse>>> GetDeletedSkills(
        [FromQuery] string? search,
        [FromQuery] int page, [FromQuery] int pageSize)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new { Message = "Page number and page size must be greater than zero." });
        }

        var query = context.Skills.IgnoreSoftDelete()
            .Where(skill => skill.IsDeleted);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(skill => skill.Name.Contains(search));
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var skills = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var response = new
        {
            Items = skills,
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
    public async Task<ActionResult<SkillResponse>> RestoreSkill(Guid id)
    {
        var skill = await context.Skills.IgnoreSoftDelete()
            .SingleOrDefaultAsync(skill => skill.Id == id);

        if (skill == null)
        {
            return NotFound(new { Message = "Skill not found." });
        }

        skill.IsDeleted = false;
        await context.SaveChangesAsync();

        return Ok(new SkillResponse
        {
            Id = skill.Id,
            Name = skill.Name,
            Description = skill.Description
        });
    }
}