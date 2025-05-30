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
[Authorize(Roles = "Business, FreelanceRecruiter", Policy = "RecruiterVerifiedOnly")]
public class CampaignController(ApplicationDbContext context) : ControllerBase
{
    [HttpPost]
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
            Status = request.Status,
            RecruiterId = Guid.Parse(recruiterId)
        };

        context.Campaigns.Add(campaign);
        await context.SaveChangesAsync();

        return Ok(new { Message = $"Create campaign successfully, campaignId: {campaign.Id}" });
    }


    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CampaignResponse>> GetCampaign(Guid id)
    {
        var campaign = await context.Campaigns
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == id);

        if (campaign == null)
        {
            return NotFound(new { Message = "Campaign not found." });
        }

        var countJobs = await context.Jobs.CountAsync(j => j.CampaignId == id);

        var response = new CampaignResponse
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            // Open = campaign.Open,
            // Close = campaign.Close,
            CountJobs = countJobs,
            Status = campaign.Status
        };

        return Ok(response);
    }


    [HttpGet]
    public async Task<ActionResult<IEnumerable<CampaignResponse>>> GetOwnCampaigns(
        [FromQuery] string? search,
        [FromQuery] CampaignStatus? status,
        [FromQuery] bool? newApplications,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new { Message = "Page number and page size must be greater than zero." });
        }

        var recruiterIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(recruiterIdStr, out var recruiterId))
        {
            return Unauthorized(new { Message = "Invalid or missing User ID in access token." });
        }

        var query = context.Campaigns.Where(c => c.RecruiterId == recruiterId);

        if (newApplications == true)
        {
            query = query.Include(c => c.Jobs)
                .ThenInclude(j => j.Applications)
                .Where(c => c.Jobs.Any(j => j.Applications.Any(a => a.Status == ApplicationStatus.Submitted)));
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(c => c.Name.Contains(search) || c.Description.Contains(search));
        }

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status);
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var campaignList = await query
            .OrderByDescending(c => c.Created)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var campaignIds = campaignList.Select(c => c.Id).ToList();

        var jobCounts = await context.Jobs
            .Where(j => campaignIds.Contains(j.CampaignId))
            .GroupBy(j => j.CampaignId)
            .Select(g => new { CampaignId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.CampaignId, g => g.Count);

        var campaigns = campaignList.Select(c => new CampaignResponse
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            // Open = c.Open,
            // Close = c.Close,
            CountJobs = jobCounts.GetValueOrDefault(c.Id, 0),
            Status = c.Status
        }).ToList();

        return Ok(new
        {
            Items = campaigns,
            TotalItems = totalItems,
            TotalPages = totalPages,
            First = page == 1,
            Last = page == totalPages,
            PageNumber = page,
            PageSize = pageSize
        });
    }


    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateCampaign([FromRoute] Guid id,
        [FromBody] UpdateCampaignRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var recruiterIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(recruiterIdString) || !Guid.TryParse(recruiterIdString, out var recruiterId))
        {
            return Unauthorized(new { Message = "Invalid or missing recruiter ID." });
        }

        var campaign = await context.Campaigns
            .SingleOrDefaultAsync(c => c.Id == id && c.RecruiterId == recruiterId);

        if (campaign == null)
        {
            return NotFound(new { Message = "Campaign not found or you are not authorized to update it." });
        }

        campaign.Name = request.Name ?? campaign.Name;
        campaign.Description = request.Description ?? campaign.Description;
        // campaign.Open = request.Open ?? campaign.Open;
        // campaign.Close = request.Close ?? campaign.Close;
        campaign.Status = request.Status ?? campaign.Status;

        context.Campaigns.Update(campaign);
        await context.SaveChangesAsync();

        return Ok(new { Message = $"Update campaign {id} successfully." });
    }


    [HttpGet("{campaignId:guid}/jobs")]
    public async Task<ActionResult<IEnumerable<JobResponse>>> GetCampaignJobs(
        [FromRoute] Guid campaignId,
        [FromQuery] JobStatus? status,
        [FromQuery] bool isPro,
        [FromQuery] bool isPublic,
        [FromQuery] bool isExpired,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] List<Guid>? jobLevels = null,
        [FromQuery] List<Guid>? jobTypes = null,
        [FromQuery] List<Guid>? contractTypes = null)
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
            .Include(j => j.Campaign)
            .Include(j => j.Major)
            .Include(j => j.Location)
            .Include(j => j.Skills)
            .Include(j => j.ContractTypes)
            .Include(j => j.JobLevels)
            .Include(j => j.JobTypes)
            .Include(j => j.JobServices)
            .Where(j => j.CampaignId == campaignId);

        if (isPro)
        {
            query = query.Where(j => j.JobServices.Count > 0);
        }

        if (isPublic)
        {
            query = query.Where(j =>
                j.Status == JobStatus.Active
                && j.StartDate <= DateTime.UtcNow
                && j.EndDate >= DateTime.UtcNow
                && j.Campaign.Status == CampaignStatus.Opened);
        }

        if (isExpired)
        {
            query = query.Where(j => j.EndDate < DateTime.UtcNow);
        }

        if (status.HasValue)
        {
            query = query.Where(j => j.Status == status.Value);
        }

        if (jobLevels is { Count: > 0 })
        {
            query = query.Where(j => jobLevels.Any(l => j.JobLevels.Any(jl => jl.Id == l)));
        }

        if (jobTypes is { Count: > 0 })
        {
            query = query.Where(j => jobTypes.Any(t => j.JobTypes.Any(jt => jt.Id == t)));
        }

        if (contractTypes is { Count: > 0 })
        {
            query = query.Where(j => contractTypes.Any(t => j.ContractTypes.Any(ct => ct.Id == t)));
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(j => j.Title.Contains(search) || j.Description.Contains(search));
        }


        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var jobs = await query
            .OrderByDescending(j => j.Created)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var jobIds = jobs.Select(j => j.Id).ToList();

        var jobApplications = await context.Applications
            .Where(a => jobIds.Contains(a.JobId))
            .Where(a => a.Status != ApplicationStatus.Draft)
            .GroupBy(a => a.JobId)
            .Select(g => new { JobId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.JobId, g => g.Count);


        var response = jobs.Select(j => new JobResponse
        {
            Id = j.Id,
            Title = j.Title,
            Description = j.Description,
            OfficeAddress = j.OfficeAddress,
            Quantity = j.Quantity,
            Status = j.Status,
            RejectReason = j.RejectReason,
            EducationLevelRequire = j.EducationLevelRequire,
            YearOfExperience = j.YearOfExperience,
            MinSalary = j.MinSalary,
            MaxSalary = j.MaxSalary,
            IsHighlight = j.IsHighlight,
            IsHot = j.IsHot,
            IsUrgent = j.IsUrgent,
            StartDate = j.StartDate,
            EndDate = j.EndDate,
            CountApplications =
                jobApplications.GetValueOrDefault(j.Id, 0),
            Major = new MajorResponse
            {
                Id = j.Major.Id,
                Name = j.Major.Name
            },
            Location = new LocationResponse
            {
                Id = j.Location.Id,
                Name = j.Location.Name,
                Region = j.Location.Region
            },
            JobDescription = context.AttachedFiles
                .Where(f => f.Type == FileType.JobDescription && f.TargetId == j.Id)
                .Select(f => new FileResponse
                {
                    Id = f.Id,
                    TargetId = f.TargetId,
                    Name = f.Name,
                    Path = f.Path,
                    Uploaded = f.Uploaded
                })
                .SingleOrDefault(),
            Skills = j.Skills.Select(s => new SkillResponse
            {
                Id = s.Id,
                Name = s.Name,
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

        var countApplications = await context.Applications
            .CountAsync(a => a.JobId == job.Id);

        var response = new JobResponse
        {
            Id = job.Id,
            Title = job.Title,
            Description = job.Description,
            OfficeAddress = job.OfficeAddress,
            Quantity = job.Quantity,
            Status = job.Status,
            RejectReason = job.RejectReason,
            EducationLevelRequire = job.EducationLevelRequire,
            YearOfExperience = job.YearOfExperience,
            MinSalary = job.MinSalary,
            MaxSalary = job.MaxSalary,
            IsHighlight = job.IsHighlight,
            IsHot = job.IsHot,
            IsUrgent = job.IsUrgent,
            StartDate = job.StartDate,
            EndDate = job.EndDate,
            CountApplications = countApplications,
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
            JobDescription = await context.AttachedFiles
                .Where(f => f.Type == FileType.JobDescription && f.TargetId == job.Id)
                .Select(f => new FileResponse
                {
                    Id = f.Id,
                    TargetId = f.TargetId,
                    Name = f.Name,
                    Path = f.Path,
                    Uploaded = f.Uploaded
                })
                .SingleOrDefaultAsync(),
            Skills = job.Skills.Select(s => new SkillResponse
            {
                Id = s.Id,
                Name = s.Name,
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

    [HttpGet("{campaignId:guid}/applications")]
    public async Task<ActionResult<IEnumerable<ApplicationResponse>>> GetCampaignApplications(
        [FromRoute] Guid campaignId,
        [FromQuery] string? search,
        [FromQuery] bool? seen,
        [FromQuery] ApplicationProcess? process,
        [FromQuery] bool? appointment,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new { Message = "Page number and page size must be greater than zero." });
        }

        var recruiterId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty);
        var campaign = await context.Campaigns
            .SingleOrDefaultAsync(c => c.Id == campaignId && c.RecruiterId == recruiterId);

        if (campaign == null)
        {
            return BadRequest(
                new { Message = "Campaign not found or you are not authorized to view its applications." });
        }

        var query = context.Applications
            .Include(a => a.Job)
            .Include(a => a.Candidate)
            .Include(a => a.Appointment)
            .Where(a => a.Job.CampaignId == campaignId && a.Status != ApplicationStatus.Draft)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(a =>
                a.FullName.Contains(search) ||
                a.Email.Contains(search) ||
                a.PhoneNumber.Contains(search) ||
                (a.Introduction != null && a.Introduction.Contains(search)));
        }

        if (process.HasValue)
        {
            query = query.Where(a => a.Process == process.Value);
        }

        if (appointment.HasValue)
        {
            query = appointment.Value
                ? query.Where(a => a.Appointment != null)
                : query.Where(a => a.Appointment == null);
        }

        if (seen.HasValue)
        {
            query = seen.Value
                ? query.Where(a => a.Status == ApplicationStatus.Seen)
                : query.Where(a => a.Status != ApplicationStatus.Seen);
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var applications = await query
            .OrderByDescending(a => a.Created)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = applications.Select(a => new ApplicationResponse
        {
            Id = a.Id,
            UserId = a.CandidateId,
            JobId = a.JobId,
            FullName = a.FullName,
            Email = a.Email,
            PhoneNumber = a.PhoneNumber,
            Introduction = a.Introduction,
            Resume = context.AttachedFiles
                .Where(f => f.Type == FileType.Application && f.TargetId == a.Id)
                .Select(f => new FileResponse
                {
                    Id = f.Id,
                    TargetId = f.TargetId,
                    Name = f.Name,
                    Path = f.Path,
                    Uploaded = f.Uploaded
                })
                .SingleOrDefault(),
            Status = a.Status,
            Process = a.Process,
            Appointment = a.Appointment != null
                ? new AppointmentShortResponse
                {
                    Id = a.Appointment.Id,
                    StartTime = a.Appointment.StartTime,
                    EndTime = a.Appointment.EndTime,
                    Participant = null,
                    Note = a.Appointment.Note,
                    Created = a.Appointment.Created
                }
                : null,
            Submitted = a.Submitted,
            Created = a.Created,
            Modified = a.Modified
        });

        return Ok(new
        {
            Items = items,
            TotalItems = totalItems,
            TotalPages = totalPages,
            First = page == 1,
            Last = page == totalPages,
            PageNumber = page,
            PageSize = pageSize
        });
    }
}