using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Helpers;
using ProjectX.Models;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/jobs")]
[Authorize]
public class JobController(ApplicationDbContext context, IWebHostEnvironment env)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobResponseForCandidate>>> GetJobs(
        [FromQuery] string? search,
        [FromQuery] bool? companyName,
        [FromQuery] List<Guid>? jobLevels,
        [FromQuery] List<Guid>? jobTypes,
        [FromQuery] List<Guid>? contractTypes,
        [FromQuery] List<Guid>? majors,
        [FromQuery] List<Guid>? locations,
        [FromQuery] bool highlightOnly,
        [FromQuery] double? minSalary,
        [FromQuery] double? maxSalary,
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
            .ThenInclude(cd => cd!.Majors)
            .Include(j => j.Campaign)
            .ThenInclude(c => c.Recruiter)
            .ThenInclude(r => r.CompanyDetail)
            .ThenInclude(cd => cd!.Location)
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
            if (!companyName.HasValue)
            {
                query = query.Where(j =>
                    j.Title.Contains(search) ||
                    j.Description.Contains(search) ||
                    (j.Campaign.Recruiter.CompanyDetail != null &&
                     j.Campaign.Recruiter.CompanyDetail.CompanyName.Contains(search))
                );
            }
            else if (companyName.Value)
            {
                query = query.Where(j =>
                    j.Campaign.Recruiter.CompanyDetail != null &&
                    j.Campaign.Recruiter.CompanyDetail.CompanyName.Contains(search)
                );
            }
            else
            {
                query = query.Where(j => j.Title.Contains(search));
            }
        }


        if (highlightOnly)
        {
            query = query.Where(j =>
                j.IsHighlight && j.HighlightStart <= DateTime.UtcNow && j.HighlightEnd >= DateTime.UtcNow);
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

        if (majors is { Count: > 0 })
        {
            query = query.Where(j => majors.Any(m => j.MajorId == m));
        }

        if (locations is { Count: > 0 })
        {
            query = query.Where(j => locations.Any(l => j.LocationId == l));
        }

        if (minSalary.HasValue)
        {
            query = query.Where(j => j.MinSalary >= minSalary);
        }

        if (maxSalary.HasValue)
        {
            query = query.Where(j => j.MaxSalary <= maxSalary);
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var jobs = await query
            .OrderByDescending(j => j.Created)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var freelanceRecruiterRoleId = await context.Roles
            .Where(r => r.Name == "FreelanceRecruiter")
            .Select(r => r.Id)
            .SingleOrDefaultAsync();

        var freelanceRecruiterIds = await context.UserRoles
            .Where(ur => ur.RoleId == freelanceRecruiterRoleId)
            .Select(ur => ur.UserId)
            .ToListAsync();

        var items = jobs.Select(j =>
        {
            var recruiter = j.Campaign.Recruiter;
            var isFreelanceRecruiter = freelanceRecruiterIds.Contains(recruiter.Id);

            return new JobResponseForCandidate
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
                Major = new MajorResponse { Id = j.Major.Id, Name = j.Major.Name },
                Location = new LocationResponse { Id = j.Location.Id, Name = j.Location.Name },
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
                    })
                    .ToList(),
                ContractTypes = j.ContractTypes.Select(ct => new ContractTypeResponse { Id = ct.Id, Name = ct.Name })
                    .ToList(),
                JobLevels = j.JobLevels.Select(jl => new JobLevelResponse { Id = jl.Id, Name = jl.Name }).ToList(),
                JobTypes = j.JobTypes.Select(jt => new JobTypeResponse { Id = jt.Id, Name = jt.Name }).ToList(),

                FreelanceRecruiter = isFreelanceRecruiter
                    ? new FreelanceRecruiterResponse
                    {
                        Id = recruiter.Id,
                        FullName = recruiter.FullName,
                        Email = recruiter.Email ?? string.Empty,
                        ProfilePicture = recruiter.ProfilePicture,
                        LinkedInProfile = recruiter.LinkedInProfile ?? string.Empty,
                        GitHubProfile = recruiter.GitHubProfile ?? string.Empty,
                    }
                    : new FreelanceRecruiterResponse(),

                CompanyRecruiter = recruiter.CompanyDetail != null
                    ? new CompanyRecruiterResponse
                    {
                        Id = recruiter.Id,
                        CompanyName = recruiter.CompanyDetail.CompanyName,
                        ShortName = recruiter.CompanyDetail.ShortName,
                        TaxCode = recruiter.CompanyDetail.TaxCode,
                        HeadQuarterAddress = recruiter.CompanyDetail.HeadQuarterAddress,
                        Logo = recruiter.CompanyDetail.Logo,
                        ContactEmail = recruiter.CompanyDetail.ContactEmail,
                        ContactPhone = recruiter.CompanyDetail.ContactPhone,
                        Website = recruiter.CompanyDetail.Website,
                        FoundedYear = recruiter.CompanyDetail.FoundedYear,
                        Size = recruiter.CompanyDetail.Size,
                        Introduction = recruiter.CompanyDetail.Introduction,
                        Majors = recruiter.CompanyDetail.Majors
                            .Select(m => new MajorResponse
                            {
                                Id = m.Id,
                                Name = m.Name
                            })
                            .ToList(),

                        Location = new LocationResponse
                        {
                            Id = recruiter.CompanyDetail.Location.Id,
                            Name = recruiter.CompanyDetail.Location.Name,
                            Region = recruiter.CompanyDetail.Location.Region
                        }
                    }
                    : new CompanyRecruiterResponse(),

                Created = j.Created,
                Modified = j.Modified
            };
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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobResponseForCandidate>> GetJob(Guid id)
    {
        var job = await context.Jobs
            .Include(j => j.Campaign)
            .ThenInclude(c => c.Recruiter)
            .ThenInclude(r => r.CompanyDetail)
            .ThenInclude(cd => cd!.Majors)
            .Include(j => j.Campaign)
            .ThenInclude(c => c.Recruiter)
            .ThenInclude(r => r.CompanyDetail)
            .ThenInclude(cd => cd!.Location)
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

        var freelanceRecruiterRoleId = await context.Roles
            .Where(r => r.Name == "FreelanceRecruiter")
            .Select(r => r.Id)
            .SingleOrDefaultAsync();

        var isFreelanceRecruiter = await context.UserRoles
            .AnyAsync(ur => ur.UserId == job.Campaign.Recruiter.Id && ur.RoleId == freelanceRecruiterRoleId);

        var recruiter = job.Campaign.Recruiter;

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
            JobDescription = await context.AttachedFiles
                .Where(f => f.Type == TargetType.JobDescription && f.TargetId == job.Id)
                .Select(f => new FileResponse
                {
                    Id = f.Id,
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

            FreelanceRecruiter = isFreelanceRecruiter
                ? new FreelanceRecruiterResponse
                {
                    Id = recruiter.Id,
                    FullName = recruiter.FullName,
                    Email = recruiter.Email ?? string.Empty,
                    ProfilePicture = recruiter.ProfilePicture,
                    LinkedInProfile = recruiter.LinkedInProfile ?? string.Empty,
                    GitHubProfile = recruiter.GitHubProfile ?? string.Empty,
                }
                : new FreelanceRecruiterResponse(),

            CompanyRecruiter = recruiter.CompanyDetail != null
                ? new CompanyRecruiterResponse
                {
                    Id = recruiter.Id,
                    CompanyName = recruiter.CompanyDetail.CompanyName,
                    ShortName = recruiter.CompanyDetail.ShortName,
                    TaxCode = recruiter.CompanyDetail.TaxCode,
                    HeadQuarterAddress = recruiter.CompanyDetail.HeadQuarterAddress,
                    Logo = recruiter.CompanyDetail.Logo,
                    ContactEmail = recruiter.CompanyDetail.ContactEmail,
                    ContactPhone = recruiter.CompanyDetail.ContactPhone,
                    Website = recruiter.CompanyDetail.Website,
                    FoundedYear = recruiter.CompanyDetail.FoundedYear,
                    Size = recruiter.CompanyDetail.Size,
                    Introduction = recruiter.CompanyDetail.Introduction,
                    Majors = recruiter.CompanyDetail.Majors.Select(
                        m => new MajorResponse
                        {
                            Id = m.Id,
                            Name = m.Name
                        }
                    ).ToList(),
                    Location = new LocationResponse
                    {
                        Id = recruiter.CompanyDetail.Location.Id,
                        Name = recruiter.CompanyDetail.Location.Name,
                        Region = recruiter.CompanyDetail.Location.Region
                    }
                }
                : new CompanyRecruiterResponse(),

            Created = job.Created,
            Modified = job.Modified
        };

        return Ok(response);
    }

    [HttpPost("{jobId:guid}/apply")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> ApplyJob([FromRoute] Guid jobId, [FromForm] ApplicationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

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

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
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

            var resumesFolder = Path.Combine(env.WebRootPath, "resumes");
            if (!Directory.Exists(resumesFolder))
            {
                Directory.CreateDirectory(resumesFolder);
            }

            var allowedDocExtensions = new[] { ".pdf", ".docx", ".doc" };
            var registrationFileExtension = Path.GetExtension(request.Resume.FileName).ToLowerInvariant();
            if (!allowedDocExtensions.Contains(registrationFileExtension))
            {
                return BadRequest("Invalid registration file extension. Only .pdf, .docx, and .doc files are allowed.");
            }

            if (request.Resume.Length > 5 * 1024 * 1024)
            {
                return BadRequest("Registration file size exceeds the 5MB limit.");
            }

            var resumeFileName = $"{Guid.NewGuid()}{Path.GetExtension(request.Resume.FileName)}";
            var filePath = Path.Combine(resumesFolder, resumeFileName);
            await using var stream = new FileStream(filePath, FileMode.Create);
            await request.Resume.CopyToAsync(stream);

            var resume = new AttachedFile
            {
                Id = Guid.NewGuid(),
                Name = resumeFileName,
                Path = PathHelper.GetRelativePathFromAbsolute(filePath, env.WebRootPath),
                Type = TargetType.Application,
                TargetId = application.Id,
                UploadedById = Guid.Parse(userId)
            };
            context.AttachedFiles.Add(resume);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();
            return Ok(new { Message = "Application submitted successfully." });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500,
                new { Message = "An error occurred while submitting the application.", Error = ex.Message });
        }
    }

    [HttpPost("{jobId:guid}/highlight")]
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "RecruiterVerifiedOnly")]
    public async Task<IActionResult> HighlightJob([FromRoute] Guid jobId, [FromBody] HighlightRequest request)
    {
        if (request.HighlightStart < DateTime.UtcNow)
            return BadRequest(new { Message = "HighlightStart must be in the future." });
        if (request.HighlightEnd <= request.HighlightStart)
            return BadRequest(new { Message = "HighlightEnd must be after HighlightStart." });

        var days = OrderHelper.CalculateHighlightDays(request.HighlightStart, request.HighlightEnd);
        if (days is < 1 or > 30)
            return BadRequest(new { Message = "Highlight duration must be between 1 and 30 days." });

        var job = await context.Jobs.FindAsync(jobId);
        if (job == null)
        {
            return NotFound(new { Message = "Job not found." });
        }

        if (job.Status != JobStatus.Active)
        {
            return BadRequest(new { Message = "Job is not active." });
        }

        var amount = OrderHelper.CalculateHighlightCost(days);
        var order = new Order
        {
            JobId = jobId,
            Days = days,
            Amount = amount,
            StartDate = request.HighlightStart,
            EndDate = request.HighlightEnd,
            Status = OrderStatus.Pending
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        return Ok(new { OrderId = order.Id, Message = "Highlight request created successfully." });
    }

    [HttpPost]
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "RecruiterVerifiedOnly")]
    public async Task<ActionResult<JobResponse>> CreateJob([FromForm] JobRequest request)
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

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var campaign = await context.Campaigns.FindAsync(request.CampaignId);
            if (campaign == null)
            {
                return NotFound(new { Message = "Campaign not found." });
            }

            if (campaign.RecruiterId != Guid.Parse(recruiterId))
            {
                return Forbid("You are not authorized to add jobs to this campaign.");
            }

            var skills = await context.Skills
                .Where(s => request.Skills.Contains(s.Id))
                .ToListAsync();

            if (skills.Count != request.Skills.Count)
            {
                return BadRequest(new { Message = "Some skills not found." });
            }

            var contractTypes = await context.ContractTypes
                .Where(ct => request.ContractTypes.Contains(ct.Id))
                .ToListAsync();

            if (contractTypes.Count != request.ContractTypes.Count)
            {
                return BadRequest(new { Message = "Some contract types not found." });
            }

            var jobTypes = await context.JobTypes
                .Where(jt => request.JobTypes.Contains(jt.Id))
                .ToListAsync();

            if (jobTypes.Count != request.JobTypes.Count)
            {
                return BadRequest(new { Message = "Some job types not found." });
            }

            var jobLevels = await context.JobLevels
                .Where(jl => request.JobLevels.Contains(jl.Id))
                .ToListAsync();

            if (jobLevels.Count != request.JobLevels.Count)
            {
                return BadRequest(new { Message = "Some job levels not found." });
            }

            var major = await context.Majors
                .Where(m => m.Id == request.MajorId)
                .AsNoTracking()
                .SingleOrDefaultAsync();
            if (major == null)
            {
                return NotFound(new { Message = "Major not found" });
            }

            var location = await context.Locations
                .Where(l => l.Id == request.LocationId)
                .AsNoTracking()
                .SingleOrDefaultAsync();

            if (location == null)
            {
                return NotFound(new { Message = "Location not found" });
            }

            // Create a new job
            var job = new Job
            {
                Id = Guid.NewGuid(),
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
                Skills = skills,
                ContractTypes = contractTypes,
                JobLevels = jobLevels,
                JobTypes = jobTypes
            };

            context.Jobs.Add(job);

            // Process job description file if uploaded
            if (request.JobDescriptionFile != null)
            {
                var jobDescriptionsFolder = Path.Combine(env.WebRootPath, "jobDescriptions");
                if (!Directory.Exists(jobDescriptionsFolder))
                {
                    Directory.CreateDirectory(jobDescriptionsFolder);
                }

                var jobDescriptionFileName =
                    $"{Guid.NewGuid()}{Path.GetExtension(request.JobDescriptionFile.FileName)}";
                var filePath = Path.Combine(jobDescriptionsFolder, jobDescriptionFileName);

                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.JobDescriptionFile.CopyToAsync(stream);
                }

                var jobDescription = new AttachedFile
                {
                    Id = Guid.NewGuid(),
                    Name = jobDescriptionFileName,
                    Path = PathHelper.GetRelativePathFromAbsolute(filePath, env.WebRootPath),
                    Type = TargetType.JobDescription,
                    TargetId = job.Id,
                    UploadedById = Guid.Parse(recruiterId)
                };
                context.AttachedFiles.Add(jobDescription);
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
                    Id = major.Id,
                    Name = major.Name
                },
                Location = new LocationResponse
                {
                    Id = location.Id,
                    Name = location.Name
                },
                JobDescription = await context.AttachedFiles
                    .Where(f => f.Type == TargetType.JobDescription && f.TargetId == job.Id)
                    .Select(f => new FileResponse
                    {
                        Id = f.Id,
                        Name = f.Name,
                        Path = f.Path,
                        Uploaded = f.Uploaded
                    })
                    .AsNoTracking()
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

            await transaction.CommitAsync();

            return CreatedAtAction(nameof(GetJob), new { id = job.Id }, response);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500,
                new { Message = "An error occurred while creating the job.", Error = ex.Message });
        }
    }


    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "RecruiterVerifiedOnly")]
    public async Task<ActionResult<JobResponse>> UpdateJob([FromRoute] Guid id, [FromForm] UpdateJobRequest request)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
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

            // Update fields only if provided
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

            // Explicitly handle nullable numeric fields
            if (request.MinSalary.HasValue)
            {
                job.MinSalary = request.MinSalary.Value;
            }

            if (request.MaxSalary.HasValue)
            {
                job.MaxSalary = request.MaxSalary.Value;
            }

            job.EducationLevelRequire = request.EducationLevelRequire; // Assuming this is required or has a default
            job.YearOfExperience = request.YearOfExperience; // Same assumption

            // Update Major
            var majorToUpdate = await context.Majors
                .Where(m => m.Id == request.MajorId)
                .SingleOrDefaultAsync();

            if (majorToUpdate == null)
            {
                return NotFound(new { Message = "Major to update not found" });
            }

            job.MajorId = request.MajorId;

            // Update Location
            var locationToUpdate = await context.Locations
                .Where(l => l.Id == request.LocationId)
                .SingleOrDefaultAsync();
            if (locationToUpdate == null)
            {
                return NotFound(new { Message = "Location to update not found" });
            }

            job.LocationId = request.LocationId;

            // Update Skills
            if (request.Skills.Count > 0)
            {
                var skillsToUpdate = await context.Skills
                    .Where(s => request.Skills.Contains(s.Id))
                    .ToListAsync();

                if (skillsToUpdate.Count != request.Skills.Count)
                {
                    return BadRequest(new { Message = "Some skills not found." });
                }

                job.Skills = skillsToUpdate;
            }

            // Update Contract Types
            if (request.ContractTypes.Count > 0)
            {
                var contractTypesToUpdate = await context.ContractTypes
                    .Where(ct => request.ContractTypes.Contains(ct.Id))
                    .ToListAsync();

                if (contractTypesToUpdate.Count != request.ContractTypes.Count)
                {
                    return BadRequest(new { Message = "Some contract types not found." });
                }

                job.ContractTypes = contractTypesToUpdate;
            }

            // Update Job Levels
            if (request.JobLevels.Count > 0)
            {
                var jobLevelsToUpdate = await context.JobLevels
                    .Where(jl => request.JobLevels.Contains(jl.Id))
                    .ToListAsync();

                if (jobLevelsToUpdate.Count != request.JobLevels.Count)
                {
                    return BadRequest(new { Message = "Some job levels not found." });
                }

                job.JobLevels = jobLevelsToUpdate;
            }

            // Update Job Types
            if (request.JobTypes.Count > 0)
            {
                var jobTypesToUpdate = await context.JobTypes
                    .Where(jt => request.JobTypes.Contains(jt.Id))
                    .ToListAsync();

                if (jobTypesToUpdate.Count != request.JobTypes.Count)
                {
                    return BadRequest(new { Message = "Some job types not found." });
                }

                job.JobTypes = jobTypesToUpdate;
            }

            if (request.Status.HasValue)
            {
                job.Status = request.Status.Value;
            }

            // Handle Job Description File Upload
            AttachedFile? jobDescription = null;
            if (request.JobDescriptionFile != null)
            {
                var allowedExtensions = new[] { ".pdf", ".docx", ".doc" };
                var extension = Path.GetExtension(request.JobDescriptionFile.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest(new { Message = "Invalid file type. Only PDF, DOCX and DOC are allowed." });
                }

                if (request.JobDescriptionFile.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(new { Message = "File size exceeds 5MB limit." });
                }

                var jobDescriptionsFolder = Path.Combine(env.WebRootPath, "jobDescriptions");
                if (!Directory.Exists(jobDescriptionsFolder))
                {
                    Directory.CreateDirectory(jobDescriptionsFolder);
                }

                var jobDescriptionFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(jobDescriptionsFolder, jobDescriptionFileName);
                await using var stream = new FileStream(filePath, FileMode.Create);
                await request.JobDescriptionFile.CopyToAsync(stream);

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var parsedUserId))
                {
                    return Unauthorized(new { Message = "Invalid user identity." });
                }

                jobDescription = new AttachedFile
                {
                    Id = Guid.NewGuid(),
                    Name = jobDescriptionFileName,
                    Path = PathHelper.GetRelativePathFromAbsolute(filePath, env.WebRootPath),
                    Type = TargetType.JobDescription,
                    TargetId = job.Id,
                    UploadedById = parsedUserId
                };
                context.AttachedFiles.Add(jobDescription);
                await context.SaveChangesAsync();
            }

            await context.SaveChangesAsync();

            // Build Response
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
                    Id = majorToUpdate.Id,
                    Name = majorToUpdate.Name
                },
                Location = new LocationResponse
                {
                    Id = locationToUpdate.Id,
                    Name = locationToUpdate.Name
                },
                JobDescription = jobDescription != null
                    ? new FileResponse
                    {
                        Id = jobDescription.Id,
                        Name = jobDescription.Name,
                        Path = jobDescription.Path,
                        Uploaded = jobDescription.Uploaded
                    }
                    : context.AttachedFiles
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

            await transaction.CommitAsync();
            return Ok(response);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500,
                new { Message = "An error occurred while updating the job.", Errors = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "RecruiterVerifiedOnly")]
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
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "RecruiterVerifiedOnly")]
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
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "RecruiterVerifiedOnly")]
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