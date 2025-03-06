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
public class JobController(ApplicationDbContext context, IWebHostEnvironment env) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobResponse>>> GetJobs([FromQuery] string search, [FromQuery] int page,
        [FromQuery] int pageSize, [FromQuery] List<string>? jobLevels, [FromQuery] List<string>? jobTypes,
        [FromQuery] List<string>? contractTypes, [FromQuery] List<string>? majors, [FromQuery] List<string>? locations)
    {
        var query = context.Jobs
            .Include(j => j.Major)
            .Include(j => j.Location)
            .Include(j => j.Skills)
            .Include(j => j.ContractTypes)
            .Include(j => j.JobLevels)
            .Include(j => j.JobTypes)
            .Include(j => j.Applications)
            .AsQueryable();

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

        if (majors is { Count: > 0 })
        {
            query = query.Where(j => majors.Any(m => j.Major.Name == m));
        }

        if (locations is { Count: > 0 })
        {
            query = query.Where(j => locations.Any(l => j.Location.Name == l));
        }

        var jobs = await query
            .OrderByDescending(j => j.Created)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var items = jobs.Select(j => new JobResponse
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

        var response = new
        {
            Items = items,
            TotalItems = totalItems,
            TotalPages = totalPages,
            First = page == 1,
            Last = page == totalPages,
            PageNumber = page,
            PageSize = pageSize
        };

        return Ok(response);
    }

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
            .SingleOrDefaultAsync(j => j.Id == id);

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
            JobDescription = context.AttachedFiles
                .Where(f => f.Type == TargetType.JobDescription && f.TargetId == job.Id)
                .Select(f => new FileResponse
                    { Id = f.Id, Name = f.Name, Path = f.Path, UploadedById = f.UploadedById, Uploaded = f.Uploaded })
                .SingleOrDefault(),
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
    public async Task<ActionResult<JobResponse>> CreateJob([FromForm] JobRequest request)
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

        //handle file upload
        if (request.JobDescriptionFile != null)
        {
            var uploadsFolder = Path.Combine(env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var jobDescriptionFileName = $"{Guid.NewGuid()}{Path.GetExtension(request.JobDescriptionFile.FileName)}";
            var filePath = Path.Combine(uploadsFolder, jobDescriptionFileName);
            await using var stream = new FileStream(filePath, FileMode.Create);
            await request.JobDescriptionFile.CopyToAsync(stream);

            var jobDescription = new AttachedFile
            {
                Id = Guid.NewGuid(),
                Name = jobDescriptionFileName,
                Path = filePath,
                Type = TargetType.JobDescription,
                TargetId = Guid.NewGuid(),
                UploadedById = Guid.Parse(recruiterId)
            };
            context.AttachedFiles.Add(jobDescription);
            await context.SaveChangesAsync();
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
        };

        campaign.CountJobs++;
        context.Entry(campaign).State = EntityState.Modified;
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
            JobDescription = context.AttachedFiles
                .Where(f => f.Type == TargetType.JobDescription && f.TargetId == job.Id)
                .Select(f => new FileResponse
                    { Id = f.Id, Name = f.Name, Path = f.Path, UploadedById = f.UploadedById, Uploaded = f.Uploaded })
                .SingleOrDefault(),
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

    [HttpPatch("{id:Guid}")]
    public async Task<ActionResult<JobResponse>> UpdateJob(Guid id, [FromForm] UpdateJobRequest request)
    {
        var job = await context.Jobs
            .Include(j => j.Major)
            .Include(j => j.Location)
            .Include(j => j.Skills)
            .Include(j => j.ContractTypes)
            .Include(j => j.JobLevels)
            .Include(j => j.JobTypes)
            .Include(j => j.Applications)
            .SingleOrDefaultAsync(j => j.Id == id);

        if (job == null)
        {
            return NotFound(new { Message = "Job not found." });
        }

        if (!string.IsNullOrEmpty(request.Title))
        {
            job.Title = request.Title;
        }

        if (!string.IsNullOrEmpty(request.Description))
        {
            job.Description = request.Description;
        }

        if (!string.IsNullOrEmpty(request.OfficeAddress))
        {
            job.OfficeAddress = request.OfficeAddress;
        }

        if (request.Quantity.HasValue)
        {
            job.Quantity = request.Quantity.Value;
        }

        job.Level = request.Level;
        job.YearOfExperience = request.YearOfExperience;
        job.MinSalary = request.MinSalary;
        job.MaxSalary = request.MaxSalary;
        job.MajorId = request.MajorId;
        job.CampaignId = request.CampaignId;
        job.LocationId = request.LocationId;

        if (request.JobDescriptionFile != null)
        {
            var uploadsFolder = Path.Combine(env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var jobDescriptionFileName = $"{Guid.NewGuid()}{Path.GetExtension(request.JobDescriptionFile.FileName)}";
            var filePath = Path.Combine(uploadsFolder, jobDescriptionFileName);
            await using var stream = new FileStream(filePath, FileMode.Create);
            await request.JobDescriptionFile.CopyToAsync(stream);

            var jobDescription = new AttachedFile
            {
                Id = Guid.NewGuid(),
                Name = jobDescriptionFileName,
                Path = filePath,
                Type = TargetType.JobDescription,
                TargetId = job.Id,
                UploadedById = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                                          throw new InvalidOperationException())
            };
            context.AttachedFiles.Add(jobDescription);
            await context.SaveChangesAsync();
        }

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
            JobDescription = context.AttachedFiles
                .Where(f => f.Type == TargetType.JobDescription && f.TargetId == job.Id)
                .Select(f => new FileResponse
                    { Id = f.Id, Name = f.Name, Path = f.Path, UploadedById = f.UploadedById, Uploaded = f.Uploaded })
                .SingleOrDefault(),
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
}