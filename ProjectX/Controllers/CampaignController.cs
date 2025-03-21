using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Models;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/campaigns")]
[Authorize]
public class CampaignController(ApplicationDbContext context) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "BusinessVerifiedOnly")]
    public async Task<ActionResult<CampaignResponse>> CreateCampaign([FromBody] CampaignRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var recruiterId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (recruiterId == null)
        {
            return Unauthorized(new { Message = "User ID not found in access token." });
        }

        var campaign = new Campaign
        {
            Name = request.Name,
            Description = request.Description,
            Open = request.Open,
            Close = request.Close,
            Status = request.Status,
            CountJobs = 0,
            RecruiterId = Guid.Parse(recruiterId)
        };

        context.Campaigns.Add(campaign);
        await context.SaveChangesAsync();

        var response = new CampaignResponse
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            Open = campaign.Open,
            Close = campaign.Close,
            CountJobs = campaign.CountJobs,
            Status = campaign.Status
        };

        return CreatedAtAction(nameof(GetCampaign), new { id = campaign.Id }, response);
    }


    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CampaignResponse>> GetCampaign(Guid id)
    {
        var campaign = await context.Campaigns
            .Include(c => c.Jobs)
            .SingleOrDefaultAsync(c => c.Id == id);

        if (campaign == null)
        {
            return NotFound(new { Message = "Campaign not found." });
        }

        var response = new CampaignResponse
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            Open = campaign.Open,
            Close = campaign.Close,
            CountJobs = campaign.CountJobs,
            Status = campaign.Status
        };

        return Ok(response);
    }

    [HttpGet]
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "BusinessVerifiedOnly")]
    public async Task<ActionResult<IEnumerable<CampaignResponse>>> GetOwnCampaigns(
        [FromQuery] string? search, [FromQuery] string? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new { Message = "Page number and page size must be greater than zero." });
        }

        var recruiterId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (recruiterId == null)
        {
            return Unauthorized(new { Message = "User ID not found in access token." });
        }

        var query = context.Campaigns
            .Where(c => c.RecruiterId == Guid.Parse(recruiterId))
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(c => c.Name.Contains(search) || c.Description.Contains(search));
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(c => c.Status.ToString() == status);
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var campaigns = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CampaignResponse
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Open = c.Open,
                Close = c.Close,
                // IsHighlight = c.IsHighlight,
                CountJobs = c.CountJobs,
                Status = c.Status
            })
            .ToListAsync();

        var response = new
        {
            Items = campaigns,
            TotalItems = totalItems,
            TotalPages = totalPages,
            First = page == 1,
            Last = page == totalPages,
            PageNumber = page,
            PageSize = pageSize
        };

        return Ok(response);
    }

    [HttpGet("{campaignId:guid}/jobs")]
    public async Task<ActionResult<IEnumerable<JobResponse>>> GetCampaignJobs([FromRoute] Guid campaignId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null,
        [FromQuery] List<string>? jobLevels = null, [FromQuery] List<string>? jobTypes = null,
        [FromQuery] List<string>? contractTypes = null)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new { Message = "Page number and page size must be greater than zero." });
        }

        var recruiterId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty);

        var campaignExists = await context.Campaigns
            .AnyAsync(c => c.Id == campaignId && c.RecruiterId == recruiterId);

        if (!campaignExists)
        {
            return NotFound(new { Message = "Campaign not found or you are not authorized to view its jobs." });
        }

        var query = context.Jobs
            .Include(j => j.Major)
            .Include(j => j.Location)
            .Include(j => j.Skills)
            .Include(j => j.ContractTypes)
            .Include(j => j.JobLevels)
            .Include(j => j.JobTypes)
            .Where(j => j.CampaignId == campaignId);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(j => j.Title.Contains(search) || j.Description.Contains(search));
        }

        if (jobLevels is { Count: > 0 })
        {
            query = query.Where(j => jobLevels.Any(l => j.JobLevels.Any(jl => jl.Name == l)));
        }

        if (jobTypes is { Count: > 0 })
        {
            query = query.Where(j => jobTypes.Any(t => j.JobTypes.Any(jt => jt.Name == t)));
        }

        if (contractTypes is { Count: > 0 })
        {
            query = query.Where(j => contractTypes.Any(t => j.ContractTypes.Any(ct => ct.Name == t)));
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var jobs = await query
            .OrderByDescending(j => j.Created)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var response = jobs.Select(j => new JobResponse
        {
            Id = j.Id,
            Title = j.Title,
            Description = j.Description,
            OfficeAddress = j.OfficeAddress,
            Quantity = j.Quantity,
            Status = j.Status,
            EducationLevelRequire = j.EducationLevelRequire,
            YearOfExperience = j.YearOfExperience,
            MinSalary = j.MinSalary,
            MaxSalary = j.MaxSalary,
            IsHighlight = j.IsHighlight,
            HighlightStart = j.HighlightStart,
            HighlightEnd = j.HighlightEnd,
            Major = new MajorResponse
            {
                Id = j.Major.Id,
                Name = j.Major.Name
            },
            Location = new LocationResponse
            {
                Id = j.Location.Id,
                Name = j.Location.Name
            },
            JobDescription = context.AttachedFiles
                .Where(f => f.Type == TargetType.JobDescription && f.TargetId == j.Id)
                .Select(f => new FileResponse
                {
                    Id = f.Id,
                    Name = f.Name,
                    Path = f.Path,
                    Uploaded = f.Uploaded
                })
                .SingleOrDefault(),
            Skills = j.Skills.Select(s => new SkillResponse
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description
            }).ToList(),
            ContractTypes = j.ContractTypes.Select(ct => new ContractTypeResponse
            {
                Id = ct.Id,
                Name = ct.Name
            }).ToList(),
            JobLevels = j.JobLevels.Select(jl => new JobLevelResponse
            {
                Id = jl.Id,
                Name = jl.Name
            }).ToList(),
            JobTypes = j.JobTypes.Select(jt => new JobTypeResponse
            {
                Id = jt.Id,
                Name = jt.Name
            }).ToList(),
            Created = j.Created,
            Modified = j.Modified
        });

        return Ok(new
        {
            Items = response,
            TotalItems = totalItems,
            TotalPages = totalPages,
            First = page == 1,
            Last = page == totalPages,
            PageNumber = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{campaignId:guid}/jobs/{jobId:guid}")]
    public async Task<ActionResult<JobResponse>> GetJobForRecruiter([FromRoute] Guid campaignId, [FromRoute] Guid jobId)
    {
        var recruiterId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty);
        var campaignExists = await context.Campaigns
            .AnyAsync(c => c.Id == campaignId && c.RecruiterId == recruiterId);

        if (!campaignExists)
        {
            return NotFound(new { Message = "Campaign not found or you are not authorized to view its jobs." });
        }

        var job = await context.Jobs
            .Include(j => j.Major)
            .Include(j => j.Location)
            .Include(j => j.Skills)
            .Include(j => j.ContractTypes)
            .Include(j => j.JobLevels)
            .Include(j => j.JobTypes)
            .Where(j => j.CampaignId == campaignId && j.Id == jobId)
            .SingleOrDefaultAsync();

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
            EducationLevelRequire = job.EducationLevelRequire,
            YearOfExperience = job.YearOfExperience,
            MinSalary = job.MinSalary,
            MaxSalary = job.MaxSalary,
            IsHighlight = job.IsHighlight,
            HighlightStart = job.HighlightStart,
            HighlightEnd = job.HighlightEnd,
            Major = new MajorResponse
            {
                Id = job.Major.Id,
                Name = job.Major.Name
            },
            Location = new LocationResponse
            {
                Id = job.Location.Id,
                Name = job.Location.Name
            },
            JobDescription = context.AttachedFiles
                .Where(f => f.Type == TargetType.JobDescription && f.TargetId == job.Id)
                .Select(f => new FileResponse
                {
                    Id = f.Id,
                    Name = f.Name,
                    Path = f.Path,
                    Uploaded = f.Uploaded
                })
                .SingleOrDefault(),
            Skills = job.Skills.Select(s => new SkillResponse
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description
            }).ToList(),
            ContractTypes = job.ContractTypes.Select(ct => new ContractTypeResponse
            {
                Id = ct.Id,
                Name = ct.Name
            }).ToList(),
            JobLevels = job.JobLevels.Select(jl => new JobLevelResponse
            {
                Id = jl.Id,
                Name = jl.Name
            }).ToList(),
            JobTypes = job.JobTypes.Select(jt => new JobTypeResponse
            {
                Id = jt.Id,
                Name = jt.Name
            }).ToList(),
            Created = job.Created,
            Modified = job.Modified
        };

        return Ok(response);
    }
}