using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Models;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/job-levels")]
[Authorize(Policy = "EmailConfirmed")]
public class JobLevelController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<JobLevelResponse>>> GetJobLevels(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page <= 0 || pageSize < 0)
        {
            return BadRequest(new
                { Message = "Page number must be greater than zero, and page size must be zero or greater." });
        }

        var query = context.JobLevels.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(jobLevel => jobLevel.Name.Contains(search));
        }

        var totalItems = await query.CountAsync();

        if (pageSize == 0)
        {
            var allJobLevels = await query
                .Select(jobLevel => new JobLevelResponse
                {
                    Id = jobLevel.Id,
                    Name = jobLevel.Name
                })
                .ToListAsync();

            return Ok(new
            {
                Items = allJobLevels,
                TotalItems = totalItems,
                TotalPages = 1,
                First = true,
                Last = true,
                PageNumber = 1,
                PageSize = totalItems
            });
        }

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var jobLevels = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(jobLevel => new JobLevelResponse
            {
                Id = jobLevel.Id,
                Name = jobLevel.Name
            })
            .ToListAsync();

        return Ok(new
        {
            Items = jobLevels,
            TotalItems = totalItems,
            TotalPages = totalPages,
            First = page == 1,
            Last = page == totalPages,
            PageNumber = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobLevelResponse>> GetJobLevel(Guid id)
    {
        var jobLevel = await context.JobLevels.FindAsync(id);

        if (jobLevel == null)
        {
            return NotFound(new { Message = $"Job level with id: {id} not found." });
        }

        return Ok(new JobLevelResponse
        {
            Id = jobLevel.Id,
            Name = jobLevel.Name
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<JobTypeResponse>> CreateJobLevel(
        [FromBody] JobLevelRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var jobLevel = new JobLevel
        {
            Name = request.Name
        };

        context.JobLevels.Add(jobLevel);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetJobLevel), new { id = jobLevel.Id }, new JobLevelResponse
        {
            Id = jobLevel.Id,
            Name = jobLevel.Name
        });
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<JobLevelResponse>> UpdateJobLevel(
        Guid id, [FromBody] UpdateJobLevelRequest request)
    {
        var jobLevel = await context.JobLevels.FindAsync(id);

        if (jobLevel == null)
        {
            return NotFound(new { Message = $"Job level with id: {id} not found." });
        }

        jobLevel.Name = request.Name;
        jobLevel.Modified = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(new JobLevelResponse
        {
            Id = jobLevel.Id,
            Name = jobLevel.Name
        });
    }


    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteJobLevel(Guid id)
    {
        var jobLevel = await context.JobLevels.FindAsync(id);

        if (jobLevel == null)
        {
            return NotFound(new { Message = $"Job level with id: {id} not found." });
        }

        context.JobLevels.Remove(jobLevel);
        await context.SaveChangesAsync();

        return Ok(new { Message = "Job level deleted successfully." });
    }

    [HttpGet("deleted")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<JobLevelResponse>>> GetDeletedJobTypes(
        [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new { Message = "Page number and page size must be greater than zero." });
        }

        var query = context.JobLevels.IgnoreSoftDelete().Where(jobLevel => jobLevel.IsDeleted);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(jobLevel => jobLevel.Name.Contains(search));
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var jobLevels = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(jobLevel => new JobLevelResponse
            {
                Id = jobLevel.Id,
                Name = jobLevel.Name
            })
            .ToListAsync();

        var response = new
        {
            Items = jobLevels,
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
    public async Task<ActionResult<JobLevelResponse>> RestoreJobLevel(Guid id)
    {
        var jobLevel = await context.JobLevels.FindAsync(id);

        if (jobLevel == null)
        {
            return NotFound(new { Message = $"Job level with id: {id} not found." });
        }

        jobLevel.IsDeleted = false;
        jobLevel.Modified = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(new JobLevelResponse
        {
            Id = jobLevel.Id,
            Name = jobLevel.Name
        });
    }
}