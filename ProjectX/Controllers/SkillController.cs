using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Models;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/skills")]
public class SkillController(
    ApplicationDbContext context,
    ILogger<SkillController> logger)
    : ControllerBase
{
    private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly ILogger<SkillController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Retrieves all skills with optional pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<SkillResponse>>> GetSkills(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var validatedPageSize = Math.Min(pageSize, 100);
            var skills = await _context.Skills
                .AsNoTracking()
                .Skip((page - 1) * validatedPageSize)
                .Take(validatedPageSize)
                .Select(skill => new SkillResponse
                {
                    Id = skill.Id,
                    Name = skill.Name,
                    Description = skill.Description,
                })
                .ToListAsync();

            var totalCount = await _context.Skills.CountAsync();
            Response.Headers.Append("X-Total-Count", totalCount.ToString());

            return Ok(skills);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving skills");
            return StatusCode(500, "An error occurred while retrieving skills");
        }
    }

    /// <summary>
    /// Retrieves a specific skill by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SkillResponse>> GetSkill(Guid id)
    {
        try
        {
            var skill = await _context.Skills
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (skill == null)
            {
                _logger.LogWarning("Skill with ID {SkillId} not found", id);
                return NotFound($"Skill with ID {id} not found");
            }

            var response = new SkillResponse
            {
                Id = skill.Id,
                Name = skill.Name,
                Description = skill.Description,
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving skill with ID {SkillId}", id);
            return StatusCode(500, "An error occurred while retrieving the skill");
        }
    }

    /// <summary>
    /// Creates a new skill
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SkillResponse>> CreateSkill([FromBody] CreateSkillRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingSkill = await _context.Skills
                .AnyAsync(s => s.Name.Equals(request.Name, StringComparison.CurrentCultureIgnoreCase));

            if (existingSkill)
            {
                return BadRequest($"A skill with the name '{request.Name}' already exists");
            }

            var skill = new Skill
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                Modified = DateTime.UtcNow
            };

            _context.Skills.Add(skill);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new skill with ID {SkillId}", skill.Id);

            var response = new SkillResponse
            {
                Id = skill.Id,
                Name = skill.Name,
                Description = skill.Description,
            };

            return CreatedAtAction(nameof(GetSkill), new { id = skill.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating skill");
            return StatusCode(500, "An error occurred while creating the skill");
        }
    }

    /// <summary>
    /// Updates an existing skill
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateSkill(Guid id, [FromBody] UpdateSkillRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var skill = await _context.Skills.FindAsync(id);

            if (skill == null)
            {
                _logger.LogWarning("Attempted to update non-existent skill with ID {SkillId}", id);
                return NotFound($"Skill with ID {id} not found");
            }

            var nameExists = await _context.Skills
                .AnyAsync(s => s.Id != id && s.Name.Equals(request.Name, StringComparison.CurrentCultureIgnoreCase));

            if (nameExists)
            {
                return BadRequest($"A skill with the name '{request.Name}' already exists");
            }

            skill.Name = request.Name.Trim();
            skill.Description = request.Description?.Trim();
            skill.Modified = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated skill with ID {SkillId}", id);

            return NoContent();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error updating skill with ID {SkillId}", id);
            return StatusCode(500, "A concurrency error occurred while updating the skill");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating skill with ID {SkillId}", id);
            return StatusCode(500, "An error occurred while updating the skill");
        }
    }

    /// <summary>
    /// Deletes a specific skill
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteSkill(Guid id)
    {
        try
        {
            var skill = await _context.Skills.FindAsync(id);
            
            if (skill == null)
            {
                _logger.LogWarning("Attempted to delete non-existent skill with ID {SkillId}", id);
                return NotFound($"Skill with ID {id} not found");
            }

            _context.Skills.Remove(skill);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted skill with ID {SkillId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting skill with ID {SkillId}", id);
            return StatusCode(500, "An error occurred while deleting the skill");
        }
    }
}