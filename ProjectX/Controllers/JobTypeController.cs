using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Models;

namespace ProjectX.Controllers;

[ApiController]
[Authorize]
[Route("capablanca/api/v0/job-types")]
public class JobTypeController(ApplicationDbContext context) : ControllerBase
{
   [HttpGet]
    public async Task<ActionResult<IEnumerable<JobTypeResponse>>> GetJobTypes(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10) 
    {
        if (page <= 0 || pageSize < 0)
        {
            return BadRequest(new { Message = "Page number must be greater than zero, and page size must be zero or greater." });
        }

        var query = context.JobTypes.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(jobType => jobType.Name.Contains(search));
        }

        var totalItems = await query.CountAsync();

        if (pageSize == 0)
        {
            var allJobTypes = await query
                .Select(jobType => new JobTypeResponse
                {
                    Id = jobType.Id,
                    Name = jobType.Name
                })
                .ToListAsync();

            return Ok(new
            {
                Items = allJobTypes,
                TotalItems = totalItems,
                TotalPages = 1,
                First = true,
                Last = true,
                PageNumber = 1,
                PageSize = totalItems
            });
        }

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var jobTypes = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(jobType => new JobTypeResponse
            {
                Id = jobType.Id,
                Name = jobType.Name
            })
            .ToListAsync();

        return Ok(new
        {
            Items = jobTypes,
            TotalItems = totalItems,
            TotalPages = totalPages,
            First = page == 1,
            Last = page == totalPages,
            PageNumber = page,
            PageSize = pageSize
        });
    }



    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobTypeResponse>> GetJobType(Guid id)
    {
        var jobType = await context.JobTypes.FindAsync(id);

        if (jobType == null)
        {
            return NotFound(new { Message = "Job type not found." });
        }

        return Ok(new JobTypeResponse
        {
            Id = jobType.Id,
            Name = jobType.Name
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<JobTypeResponse>> CreateJobType(
        [FromBody] JobTypeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var jobType = new JobType
        {
            Name = request.Name
        };

        context.JobTypes.Add(jobType);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetJobType), new { id = jobType.Id }, new JobTypeResponse
        {
            Id = jobType.Id,
            Name = jobType.Name
        });
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<JobTypeResponse>> UpdateJobType(
        Guid id, [FromBody] UpdateJobTypeRequest request)
    {
        var jobType = await context.JobTypes.FindAsync(id);

        if (jobType == null)
        {
            return NotFound(new { Message = "Job type not found." });
        }

        jobType.Name = request.Name;
        jobType.Modified = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(new JobTypeResponse
        {
            Id = jobType.Id,
            Name = jobType.Name
        });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteJobType(Guid id)
    {
        var jobType = await context.JobTypes.FindAsync(id);

        if (jobType == null)
        {
            return NotFound(new { Message = "Job type not found." });
        }

        context.JobTypes.Remove(jobType);
        await context.SaveChangesAsync();

        return Ok(new { Message = "Job type deleted successfully." });
    }

    [HttpGet("deleted")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<JobTypeResponse>>> GetDeletedJobTypes(
        [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new { Message = "Page number and page size must be greater than zero." });
        }

        var query = context.JobTypes.IgnoreSoftDelete()
            .Where(jobType => jobType.IsDeleted);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(jobType => jobType.Name.Contains(search));
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var jobTypes = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(jobType => new JobTypeResponse
            {
                Id = jobType.Id,
                Name = jobType.Name
            })
            .ToListAsync();

        var response = new
        {
            Items = jobTypes,
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
    public async Task<ActionResult<JobTypeResponse>> RestoreJobType(Guid id)
    {
        var jobType = await context.JobTypes.IgnoreSoftDelete()
            .SingleOrDefaultAsync(jobType => jobType.Id == id);

        if (jobType == null)
        {
            return NotFound(new { Message = "Job type not found." });
        }

        jobType.IsDeleted = false;
        await context.SaveChangesAsync();

        return Ok(new JobTypeResponse
        {
            Id = jobType.Id,
            Name = jobType.Name
        });
    }
}