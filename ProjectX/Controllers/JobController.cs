using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Models;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/jobs")]
[Authorize]
public class JobController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet("{id:Guid}")]
    public async Task<ActionResult<JobResponse>> GetJob(Guid id)
    {
        var job = await context.Jobs
            .Include(j => j.Major)
            .Include(j => j.Location)
            .Include(j => j.Skills)
            .Include(j => j.ContractTypes)
            .Include(j => j.JobLevels)
            .Include(j => j.JobTypes)
            .Include(j => j.Applications)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null)
        {
            return NotFound(new { Message = "Job not found." });
        }

        var response = new JobResponse
        {
            Id = job.Id,
            Title = job.Title,
            Description = job.Description,
            OfficeAddress = job.OfficeAddress,
            Quantity = job.Quantity,
            Status = job.Status,
            Level = job.Level,
            YearOfExperience = job.YearOfExperience,
            MinSalary = job.MinSalary,
            MaxSalary = job.MaxSalary,
            Major = job.Major,
            Location = job.Location,
            JobDescriptionId = job.JobDescriptionId,
            Skills = job.Skills,
            ContractTypes = job.ContractTypes,
            JobLevels = job.JobLevels,
            JobTypes = job.JobTypes,
            Applications = job.Applications,
            Created = job.Created,
            Modified = job.Modified
        };

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "BusinessVerifiedOnly")]
    public async Task<ActionResult<JobResponse>> CreateJob([FromBody] JobRequest request)
    {
        var recruiterId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (recruiterId == null)
        {
            return Unauthorized(new { Message = "User ID not found in access token." });
        }

        var campaign = await context.Campaigns.FindAsync(request.CampaignId);
        if (campaign == null)
        {
            return NotFound(new { Message = "Campaign not found." });
        }

        if (campaign.RecruiterId != Guid.Parse(recruiterId))
        {
            return Forbid("You are not authorized to add jobs to this campaign.");
        }

        var job = new Job
        {
            Title = request.Title,
            Description = request.Description,
            OfficeAddress = request.OfficeAddress,
            Quantity = request.Quantity,
            Level = request.Level,
            YearOfExperience = request.YearOfExperience,
            MinSalary = request.MinSalary,
            MaxSalary = request.MaxSalary,
            MajorId = request.MajorId,
            CampaignId = request.CampaignId,
            LocationId = request.LocationId,
            JobDescriptionId = request.JobDescriptionId
        };

        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        var response = new JobResponse
        {
            Id = job.Id,
            Title = job.Title,
            Description = job.Description,
            OfficeAddress = job.OfficeAddress,
            Quantity = job.Quantity,
            Status = job.Status,
            Level = job.Level,
            YearOfExperience = job.YearOfExperience,
            MinSalary = job.MinSalary,
            MaxSalary = job.MaxSalary,
            Major = job.Major,
            Location = job.Location,
            JobDescriptionId = job.JobDescriptionId,
            Skills = job.Skills,
            ContractTypes = job.ContractTypes,
            JobLevels = job.JobLevels,
            JobTypes = job.JobTypes,
            Applications = job.Applications,
            Created = job.Created,
            Modified = job.Modified
        };

        return CreatedAtAction(nameof(GetJob), new { id = job.Id }, response);
    }
}