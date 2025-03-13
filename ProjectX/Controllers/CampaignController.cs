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
            // IsHighlight = request.IsHighlight,
            // HighlightStart = request.HighlightStart,
            // HighlightEnd = request.HighlightEnd,
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
            // IsHighlight = campaign.IsHighlight,
            // IsUrgent = campaign.IsUrgent,
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
            .FirstOrDefaultAsync(c => c.Id == id);

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
            // IsHighlight = campaign.IsHighlight,
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
        var campaign = await context.Campaigns
            .Include(c => c.Jobs)
            .ThenInclude(j => j.Major)
            .Include(c => c.Jobs)
            .ThenInclude(j => j.Location)
            .Include(c => c.Jobs)
            .ThenInclude(j => j.Skills)
            .Include(c => c.Jobs)
            .ThenInclude(j => j.ContractTypes)
            .Include(c => c.Jobs)
            .ThenInclude(j => j.JobLevels)
            .Include(c => c.Jobs)
            .ThenInclude(j => j.JobTypes)
            .Include(c => c.Jobs)
            .ThenInclude(j => j.Applications)
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.RecruiterId == recruiterId);

        if (campaign == null)
        {
            return NotFound(new { Message = "Campaign not found or you are not authorized to view its jobs." });
        }

        var query = campaign.Jobs.AsQueryable();

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

        var totalItems = query.Count();
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
            Level = j.Level,
            YearOfExperience = j.YearOfExperience,
            MinSalary = j.MinSalary,
            MaxSalary = j.MaxSalary,
            IsHighlight = j.IsHighlight,
            HighlightStart = j.HighlightStart,
            HighlightEnd = j.HighlightEnd,
            Major = j.Major,
            Location = j.Location,
            JobDescription = context.AttachedFiles
                .Where(f => f.Type == TargetType.JobDescription && f.TargetId == j.Id)
                .Select(f => new FileResponse
                    { Id = f.Id, Name = f.Name, Path = f.Path, UploadedById = f.UploadedById, Uploaded = f.Uploaded })
                .SingleOrDefault(),
            Skills = j.Skills,
            ContractTypes = j.ContractTypes,
            JobLevels = j.JobLevels,
            JobTypes = j.JobTypes,
            Applications = j.Applications,
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

    // [HttpGet("highlight")]
    // public async Task<ActionResult<IEnumerable<CampaignResponse>>> GetHighlightCampaigns()
    // {
    //     
    // }
}