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
    public async Task<ActionResult<IEnumerable<JobResponseForCandidate>>> GetJobs(
        [FromQuery] string? search,
        [FromQuery] List<string>? jobLevels,
        [FromQuery] List<string>? jobTypes,
        [FromQuery] List<string>? contractTypes,
        [FromQuery] List<string>? majors,
        [FromQuery] List<string>? locations,
        [FromQuery] int pageSize = 10,
        [FromQuery] int page = 1)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new { Message = "Page number and page size must be greater than zero." });
        }

        var query = context.Jobs
            .Include(j => j.Campaign)
            .ThenInclude(c => c.Recruiter)
            .ThenInclude(r => r.CompanyDetail)
            .Include(j => j.Major)
            .Include(j => j.Location)
            .Include(j => j.Skills)
            .Include(j => j.ContractTypes)
            .Include(j => j.JobLevels)
            .Include(j => j.JobTypes)
            .Where(j => j.Status == JobStatus.Active && j.Campaign.Status == CampaignStatus.Opened)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(j =>
                j.Campaign.Recruiter.CompanyDetail != null &&
                (j.Title.Contains(search) || j.Description.Contains(search) ||
                 j.Campaign.Recruiter.CompanyDetail.CompanyName.Contains(search)));
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

        var items = jobs.Select(j => new JobResponseForCandidate
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
            Recruiter = new RecruiterResponse
            {
                Id = j.Campaign.Recruiter.Id,
                CompanyName = j.Campaign.Recruiter.CompanyDetail!.CompanyName,
                HeadQuarterAddress = j.Campaign.Recruiter.CompanyDetail.HeadQuarterAddress,
                Logo = j.Campaign.Recruiter.CompanyDetail.Logo,
                ContactEmail = j.Campaign.Recruiter.CompanyDetail.ContactEmail,
                FoundedYear = j.Campaign.Recruiter.CompanyDetail.FoundedYear,
                Introduction = j.Campaign.Recruiter.CompanyDetail.Introduction
            },
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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobResponseForCandidate>> GetJob(Guid id)
    {
        var job = await context.Jobs
            .Include(j => j.Campaign)
            .ThenInclude(c => c.Recruiter)
            .ThenInclude(r => r.CompanyDetail)
            .Include(j => j.Major)
            .Include(j => j.Location)
            .Include(j => j.Skills)
            .Include(j => j.ContractTypes)
            .Include(j => j.JobLevels)
            .Include(j => j.JobTypes)
            .SingleOrDefaultAsync(j => j.Id == id);

        if (job == null)
        {
            return NotFound(new { Message = "Job not found." });
        }

        var response = new JobResponseForCandidate
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
            Recruiter = new RecruiterResponse
            {
                Id = job.Campaign.Recruiter.Id,
                CompanyName = job.Campaign.Recruiter.CompanyDetail!.CompanyName,
                HeadQuarterAddress = job.Campaign.Recruiter.CompanyDetail.HeadQuarterAddress,
                Logo = job.Campaign.Recruiter.CompanyDetail.Logo,
                ContactEmail = job.Campaign.Recruiter.CompanyDetail.ContactEmail,
                FoundedYear = job.Campaign.Recruiter.CompanyDetail.FoundedYear,
                Introduction = job.Campaign.Recruiter.CompanyDetail.Introduction
            },
            Created = job.Created,
            Modified = job.Modified
        };

        return Ok(response);
    }

    [HttpPost("{jobId:guid}/apply")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> ApplyJob([FromRoute] Guid jobId, [FromForm] ApplicationRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized(new { Message = "User ID not found in access token." });
        }

        var job = await context.Jobs.FindAsync(jobId);
        if (job == null)
        {
            return NotFound(new { Message = "Job not found." });
        }

        var application = new Application
        {
            JobId = jobId,
            CandidateId = Guid.Parse(userId),
            FullName = request.FullName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Introduction = request.Introduction,
            Status = request.Status,
            Process = ApplicationProcess.Pending
        };

        context.Applications.Add(application);
        await context.SaveChangesAsync();

        var uploadsFolder = Path.Combine(env.WebRootPath, "uploads");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var resumeFileName = $"{Guid.NewGuid()}{Path.GetExtension(request.Resume.FileName)}";
        var filePath = Path.Combine(uploadsFolder, resumeFileName);
        await using var stream = new FileStream(filePath, FileMode.Create);
        await request.Resume.CopyToAsync(stream);

        var resume = new AttachedFile
        {
            Id = Guid.NewGuid(),
            Name = resumeFileName,
            Path = filePath,
            Type = TargetType.Application,
            TargetId = application.Id,
            UploadedById = Guid.Parse(userId)
        };
        context.AttachedFiles.Add(resume);
        await context.SaveChangesAsync();

        return Ok(new { Message = "Application submitted successfully." });
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
            EducationLevelRequire = request.EducationLevelRequire,
            YearOfExperience = request.YearOfExperience,
            MinSalary = request.MinSalary,
            MaxSalary = request.MaxSalary,
            MajorId = request.MajorId,
            IsHighlight = request.IsHighlight,
            HighlightStart = request.HighlightStart,
            HighlightEnd = request.HighlightEnd,
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
            EducationLevelRequire = job.EducationLevelRequire,
            YearOfExperience = job.YearOfExperience,
            MinSalary = job.MinSalary,
            MaxSalary = job.MaxSalary,
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

        return CreatedAtAction(nameof(GetJob), new { id = job.Id }, response);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "BusinessVerifiedOnly")]
    public async Task<ActionResult<JobResponse>> UpdateJob([FromRoute] Guid id, [FromForm] UpdateJobRequest request)
    {
        var job = await context.Jobs
            .Include(j => j.Major)
            .Include(j => j.Location)
            .Include(j => j.Skills)
            .Include(j => j.ContractTypes)
            .Include(j => j.JobLevels)
            .Include(j => j.JobTypes)
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

        job.EducationLevelRequire = request.EducationLevelRequire;
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
            EducationLevelRequire = job.EducationLevelRequire,
            YearOfExperience = job.YearOfExperience,
            MinSalary = job.MinSalary,
            MaxSalary = job.MaxSalary,
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

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "BusinessVerifiedOnly")]
    public async Task<ActionResult> DeleteJob(Guid id)
    {
        var job = await context.Jobs.FindAsync(id);
        if (job == null)
        {
            return NotFound(new { Message = "Job not found." });
        }

        context.Jobs.Remove(job);
        await context.SaveChangesAsync();

        return Ok(new { Message = "Job deleted successfully." });
    }

    [HttpGet("{jobId:Guid}/applications")]
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "BusinessVerifiedOnly")]
    public async Task<ActionResult<IEnumerable<ApplicationResponse>>> GetJobApplications([FromRoute] Guid jobId,
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized(new { Message = "User ID not found in access token." });
        }

        var job = await context.Jobs
            .Include(j => j.Campaign)
            .SingleOrDefaultAsync(j => j.Id == jobId);

        if (job == null)
        {
            return NotFound(new { Message = "Job not found." });
        }

        if (Guid.Parse(userId) != job.Campaign.RecruiterId)
        {
            return Forbid("You are not authorized to view applications for this job.");
        }

        var query = context.Applications
            .Include(a => a.Job)
            .ThenInclude(j => j.Campaign)
            .Where(a => a.JobId == jobId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(a =>
                a.Introduction != null && (a.FullName.Contains(search) || a.Introduction.Contains(search) ||
                                           a.PhoneNumber.Contains(search) || a.Email.Contains(search)));
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var applications = await query
            .OrderByDescending(a => a.Created)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = applications.Select(async a => new ApplicationResponse
        {
            Id = a.Id,
            FullName = a.FullName,
            Email = a.Email,
            PhoneNumber = a.PhoneNumber,
            Introduction = a.Introduction,
            Resume = await context.AttachedFiles
                .Where(f => f.Type == TargetType.Application && f.TargetId == a.Id)
                .Select(f => new FileResponse
                {
                    Id = f.Id,
                    Name = f.Name,
                    Path = f.Path,
                    Uploaded = f.Uploaded
                })
                .SingleOrDefaultAsync(),
            Status = a.Status,
            Process = a.Process,
            Applied = a.Created,
            Submitted = a.Submitted,
            Created = a.Created,
            Modified = a.Modified
        });

        return Ok(new
        {
            TotalItems = totalItems,
            TotalPages = totalPages,
            First = page == 1,
            Last = page == totalPages,
            PageNumber = page,
            PageSize = pageSize,
            Items = items
        });
    }

    [HttpGet("{jobId:Guid}/applications/{applicationId:Guid}")]
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "BusinessVerifiedOnly")]
    public async Task<ActionResult<ApplicationResponse>> GetJobApplication([FromRoute] Guid jobId,
        [FromRoute] Guid applicationId)
    {
        var application = await context.Applications
            .SingleOrDefaultAsync(a => a.Id == applicationId && a.JobId == jobId);

        if (application == null)
        {
            return NotFound(new { Message = "Application not found." });
        }

        var response = new ApplicationResponse
        {
            Id = application.Id,
            FullName = application.FullName,
            Email = application.Email,
            PhoneNumber = application.PhoneNumber,
            Introduction = application.Introduction,
            Resume = await context.AttachedFiles
                .Where(f => f.Type == TargetType.Application && f.TargetId == application.Id)
                .Select(f => new FileResponse
                {
                    Id = f.Id,
                    Name = f.Name,
                    Path = f.Path,
                    Uploaded = f.Uploaded
                })
                .SingleOrDefaultAsync(),
            Status = application.Status,
            Process = application.Process,
            Applied = application.Created,
            Submitted = application.Submitted,
            Created = application.Created,
            Modified = application.Modified
        };

        return Ok(response);
    }
}