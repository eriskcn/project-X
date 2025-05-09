using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Helpers;
using ProjectX.Models;
using ProjectX.Services.Notifications;
using ProjectX.Services.Stats;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/jobs")]
[Authorize]
public class JobController(
    ApplicationDbContext context,
    IWebHostEnvironment env,
    IStatsService statsService,
    INotificationService notificationService)
    : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<JobResponseForCandidate>>> GetJobs(
        [FromQuery] string? search,
        [FromQuery] bool? companyName,
        [FromQuery] List<Guid>? jobLevels,
        [FromQuery] List<Guid>? jobTypes,
        [FromQuery] List<Guid>? contractTypes,
        [FromQuery] List<Guid>? majors,
        [FromQuery] List<Guid>? locations,
        [FromQuery] double? minSalary,
        [FromQuery] double? maxSalary,
        [FromQuery] double? minExp,
        [FromQuery] double? maxExp,
        [FromQuery] bool isHighlight = false,
        [FromQuery] bool isUrgent = false,
        [FromQuery] bool isHot = false,
        [FromQuery] int pageSize = 10,
        [FromQuery] int page = 1)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new { Message = "Page number and page size must be greater than zero." });
        }

        if (minSalary > maxSalary)
            return BadRequest(new { Message = "minSalary must be <= maxSalary." });

        var savedJobIds = new List<Guid>();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            savedJobIds = await context.Users
                .Where(u => u.Id == Guid.Parse(userId))
                .SelectMany(u => u.SavedJobs)
                .Select(j => j.Id)
                .ToListAsync();
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
            .Where(j => j.Status == JobStatus.Active
                        && j.Campaign.Status == CampaignStatus.Opened
                        && j.StartDate <= DateTime.UtcNow
                        && j.EndDate >= DateTime.UtcNow)
            .AsQueryable();

        if (minExp > maxExp)
            return BadRequest("minExp must be less than or equal to maxExp.");

        if (isHighlight)
        {
            query = query.Where(j => j.IsHighlight);
        }

        if (isUrgent)
        {
            query = query.Where(j => j.IsUrgent);
        }

        if (isHot)
        {
            query = query.Where(j => j.IsHot);
        }

        if (minExp.HasValue)
        {
            query = query.Where(j => j.YearOfExperience > minExp.Value);
        }

        if (maxExp.HasValue)
        {
            query = query.Where(j => j.YearOfExperience <= maxExp.Value);
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

        query = query
            .OrderByDescending(j => j.IsUrgent)
            .ThenByDescending(j => j.IsHot)
            .ThenByDescending(j => j.Created);

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var jobs = await query
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
                IsSaved = savedJobIds.Contains(j.Id),
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
                IsHot = j.IsHot,
                IsUrgent = j.IsUrgent,
                StartDate = j.StartDate,
                EndDate = j.EndDate,
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
                        Id = recruiter.CompanyDetail.Id,
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
    [AllowAnonymous]
    public async Task<ActionResult<JobResponseForCandidate>> GetJob(Guid id)
    {
        var isSaved = false;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId != null)
        {
            isSaved = await context.Users
                .Where(u => u.Id == Guid.Parse(userId))
                .SelectMany(u => u.SavedJobs)
                .AnyAsync(j => j.Id == id);
        }


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
            .SingleOrDefaultAsync(j => j.Status == JobStatus.Active
                                       && j.Campaign.Status == CampaignStatus.Opened
                                       && j.StartDate <= DateTime.UtcNow
                                       && j.EndDate >= DateTime.UtcNow
                                       && j.Id == id);

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
            IsSaved = isSaved,
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
            IsHot = job.IsHot,
            IsUrgent = job.IsUrgent,
            StartDate = job.StartDate,
            EndDate = job.EndDate,
            Major = new MajorResponse
            {
                Id = job.Major.Id,
                Name = job.Major.Name
            },
            Location = new LocationResponse
            {
                Id = job.Location.Id,
                Name = job.Location.Name,
                Region = job.Location.Region
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
                    Id = recruiter.CompanyDetail.Id,
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

        job.ViewCount++;
        context.Jobs.Update(job);
        await context.SaveChangesAsync();

        return Ok(response);
    }

    [HttpGet("{id:guid}/hidden-stats")]
    public async Task<IActionResult> GetHiddenStats(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized(new { Message = "Invalid user ID." });
        }

        var user = await context.Users.FindAsync(userGuid);
        if (user == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        if (user.XTokenBalance < 2)
        {
            return BadRequest(new { Message = "Not enough token do view this hidden stats." });
        }

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var job = await context.Jobs.FindAsync(id);
            if (job == null)
            {
                return NotFound(new { Message = "Job not found." });
            }

            var competitiveRate = await statsService.GetCompetitiveRateAsync(job);

            var applicationCount = await context.Applications
                .CountAsync(a => a.JobId == id && a.Status != ApplicationStatus.Draft);
            var tokenTransaction = new TokenTransaction
            {
                Id = Guid.NewGuid(),
                UserId = userGuid,
                AmountToken = 2,
                JobId = job.Id,
                Type = TokenTransactionType.ViewHiddenJobDetails,
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            };

            user.XTokenBalance -= 2;
            context.Users.Update(user);
            context.TokenTransactions.Add(tokenTransaction);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                job.ViewCount,
                ApplicationCount = applicationCount,
                CompetitiveRate = competitiveRate
            });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpPost("{jobId:guid}/apply")]
    [Authorize(Roles = "Candidate", Policy = "EmailConfirmed")]
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

        var job = await context.Jobs
            .Include(j => j.Campaign)
            .SingleOrDefaultAsync(j =>
                j.Id == jobId
                && j.Campaign.Status == CampaignStatus.Opened
                && j.Status == JobStatus.Active
                && j.StartDate <= DateTime.UtcNow
                && j.EndDate >= DateTime.UtcNow);
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
                Submitted = request.Status == ApplicationStatus.Submitted
                    ? DateTime.UtcNow
                    : null,
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

            if (request.Resume.Length > 10 * 1024 * 1024)
            {
                return BadRequest("Registration file size exceeds the 5MB limit.");
            }

            var cleanFileName = PathHelper.GetCleanFileName(request.Resume.FileName);
            var displayName = Path.GetFileName(cleanFileName);

            var resumeFileName = $"{Guid.NewGuid()}{Path.GetExtension(request.Resume.FileName)}";
            var filePath = Path.Combine(resumesFolder, resumeFileName);
            await using var stream = new FileStream(filePath, FileMode.Create);
            await request.Resume.CopyToAsync(stream);

            var resume = new AttachedFile
            {
                Id = Guid.NewGuid(),
                Name = displayName,
                Path = PathHelper.GetRelativePathFromAbsolute(filePath, env.WebRootPath),
                Type = FileType.Application,
                TargetId = application.Id,
                UploadedById = Guid.Parse(userId)
            };
            context.AttachedFiles.Add(resume);

            await notificationService.SendNotificationAsync(
                NotificationType.NewApplication,
                job.Campaign.RecruiterId,
                job.CampaignId);

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

    [HttpPost("{jobId:guid}/save")]
    [Authorize(Roles = "Candidate", Policy = "EmailConfirmed")]
    public async Task<IActionResult> SaveJob(Guid jobId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return BadRequest(new { Message = "User ID not found in access token." });
        }

        var user = await context.Users
            .Include(u => u.SavedJobs)
            .SingleOrDefaultAsync(u => u.Id == Guid.Parse(userId));
        if (user == null)
        {
            return BadRequest(new { Message = "User not found." });
        }

        var job = await context.Jobs.FindAsync(jobId);
        if (job == null)
        {
            return NotFound(new { Message = "Job not found." });
        }

        if (user.SavedJobs.Any(j => j.Id == jobId))
        {
            return Conflict(new { Message = "Job is already saved." });
        }

        user.SavedJobs.Add(job);
        await context.SaveChangesAsync();

        return Ok(new { Message = "Job saved successfully." });
    }

    [HttpDelete("{jobId:guid}/un-save")]
    [Authorize(Roles = "Candidate", Policy = "EmailConfirmed")]
    public async Task<IActionResult> UnSaveJob(Guid jobId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return BadRequest(new { Message = "User ID not found in access token." });
        }

        var user = await context.Users
            .Include(u => u.SavedJobs)
            .SingleOrDefaultAsync(u => u.Id == Guid.Parse(userId));
        if (user == null)
        {
            return BadRequest(new { Message = "User not found." });
        }

        var job = await context.Jobs.FindAsync(jobId);
        if (job == null)
        {
            return NotFound(new { Message = "Job not found." });
        }

        var savedJob = user.SavedJobs.SingleOrDefault(j => j.Id == jobId);
        if (savedJob == null)
        {
            return Conflict(new { Message = "Job is not saved." });
        }

        user.SavedJobs.Remove(savedJob);
        await context.SaveChangesAsync();

        return Ok(new { Message = "Job unsaved successfully." });
    }

    [HttpGet("saved")]
    [Authorize(Roles = "Candidate", Policy = "EmailConfirmed")]
    public async Task<ActionResult<IEnumerable<SavedJobResponse>>> GetSavedJobs(
        [FromQuery] string? search, int page = 1, int pageSize = 10)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new { Message = "Page number and page size must be greater than zero." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized(new { Message = "User ID not found in access token." });
        }

        var savedJobs = context.Users
            .Where(u => u.Id == Guid.Parse(userId))
            .SelectMany(u => u.SavedJobs)
            .Include(j => j.Major)
            .Include(j => j.Location)
            .Include(j => j.Campaign)
            .ThenInclude(c => c.Recruiter)
            .ThenInclude(r => r.CompanyDetail)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            savedJobs = savedJobs.Where(j =>
                j.Title.Contains(search) ||
                j.Description.Contains(search) ||
                (j.Campaign.Recruiter.CompanyDetail != null &&
                 j.Campaign.Recruiter.CompanyDetail.CompanyName.Contains(search)));
        }

        var totalItems = await savedJobs.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var items = await savedJobs
            .OrderByDescending(j => j.Created)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new SavedJobResponse
            {
                Id = j.Id,
                Title = j.Title,
                MinSalary = j.MinSalary,
                MaxSalary = j.MaxSalary,
                YearOfExperience = j.YearOfExperience,
                Location = new LocationResponse
                {
                    Id = j.Location.Id,
                    Name = j.Location.Name,
                    Region = j.Location.Region
                },
                Recruiter = new UserResponse
                {
                    Id = j.Campaign.Recruiter.Id,
                    Name = j.Campaign.Recruiter.CompanyDetail != null
                        ? j.Campaign.Recruiter.CompanyDetail.CompanyName
                        : j.Campaign.Recruiter.FullName,
                    ProfilePicture = j.Campaign.Recruiter.CompanyDetail != null
                        ? j.Campaign.Recruiter.CompanyDetail.Logo
                        : j.Campaign.Recruiter.ProfilePicture
                },
                Created = j.Created
            })
            .ToListAsync();

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

    [HttpPost]
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "RecruiterVerifiedOnly")]
    public async Task<IActionResult> CreateJob([FromForm] JobRequest request)
    {
        const int maxFileSize = 10 * 1024 * 1024; // 10MB limit

        // Validate request
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request.JobDescriptionFile is { Length: > maxFileSize })
        {
            return BadRequest(new { Message = "File size exceeds the 10MB limit." });
        }

        if (request.MinSalary > request.MaxSalary)
        {
            return BadRequest(new { Message = "Max salary must be greater than min salary." });
        }

        var recruiterId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(recruiterId, out var recruiterGuid))
        {
            return BadRequest(new { Message = "Invalid recruiter ID." });
        }

        var recruiter = await context.Users
            .Include(u => u.CompanyDetail)
            .ThenInclude(cd => cd!.Location)
            .Include(u => u.CompanyDetail)
            .ThenInclude(cd => cd!.Majors)
            .Include(u => u.PurchasedPackages)
            .SingleOrDefaultAsync(u => u.Id == recruiterGuid);

        if (recruiter == null)
        {
            return BadRequest(new { Message = "Recruiter not found." });
        }

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var isAutoAccept = recruiter.Level is AccountLevel.Elite or AccountLevel.Premium;
            if (request.IsDraft)
            {
                isAutoAccept = false;
            }

            var jobLevels = await context.JobLevels
                .Where(jl => request.JobLevels.Contains(jl.Id))
                .ToListAsync();

            var jobTypes = await context.JobTypes
                .Where(jt => request.JobTypes.Contains(jt.Id))
                .ToListAsync();

            var contractTypes = await context.ContractTypes
                .Where(ct => request.ContractTypes.Contains(ct.Id))
                .ToListAsync();

            var skills = await context.Skills
                .Where(s => request.Skills.Contains(s.Id))
                .ToListAsync();

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
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                CampaignId = request.CampaignId,
                MajorId = request.MajorId,
                LocationId = request.LocationId,
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow,
                Status = isAutoAccept ? JobStatus.Active : JobStatus.Pending,
                JobLevels = jobLevels,
                JobTypes = jobTypes,
                ContractTypes = contractTypes,
                Skills = skills
            };

            if (!JobHelper.IsValidJobDuration(job))
            {
                return BadRequest(new { Message = "Invalid StartDate and EndDate" });
            }

            var jobServices = new List<JobService>();
            var totalToken = 0;
            var totalCash = 0.0;

            if (request.ServiceIds is { Count: > 0 and <= 3 })
            {
                var serviceIds = request.ServiceIds;
                var services = await context.Services
                    .Where(s => serviceIds.Contains(s.Id))
                    .ToListAsync();

                var isHighlight = services.Any(s => s.Type == ServiceType.Highlight);
                var isUrgent = services.Any(s => s.Type == ServiceType.Urgent);
                var isHot = services.Any(s => s.Type == ServiceType.Hot);

                if (!JobHelper.IsValidProJob(job, isHighlight, isHot, isUrgent))
                {
                    return BadRequest(new { Message = "Invalid job duration for selected services." });
                }

                foreach (var service in services)
                {
                    var jobDuration = (int)(job.EndDate - job.StartDate).TotalDays;
                    switch (service.Type)
                    {
                        case ServiceType.Highlight:
                            totalToken += jobDuration * service.XTokenPrice;
                            var first7Days = Math.Min(jobDuration, 7);
                            var remainingDays = jobDuration - first7Days;
                            totalCash += (first7Days * service.CashPrice) +
                                         (remainingDays * service.CashPrice * Convert.ToDouble(1.2m));
                            break;
                        case ServiceType.Urgent:
                        case ServiceType.Hot:
                            totalToken += service.XTokenPrice;
                            totalCash += service.CashPrice;
                            break;
                        default:
                            continue;
                    }

                    jobServices.Add(new JobService
                    {
                        Id = Guid.NewGuid(),
                        JobId = job.Id,
                        ServiceId = service.Id,
                        IsActive = false,
                        Created = DateTime.UtcNow,
                        Modified = DateTime.UtcNow
                    });
                }

                if (jobServices.Count > 0)
                {
                    job.JobServices = jobServices;

                    if (request.PaymentMethod == JobPaymentMethod.XToken)
                    {
                        if (recruiter.XTokenBalance < totalToken)
                        {
                            return BadRequest(new { Message = "Not enough X Token." });
                        }

                        var tokenTransaction = new TokenTransaction
                        {
                            Id = Guid.NewGuid(),
                            UserId = recruiterGuid,
                            AmountToken = totalToken,
                            JobId = job.Id,
                            Type = TokenTransactionType.PurchaseJobService,
                            Created = DateTime.UtcNow,
                            Modified = DateTime.UtcNow
                        };

                        context.TokenTransactions.Add(tokenTransaction);

                        recruiter.XTokenBalance -= totalToken;
                        context.Users.Update(recruiter);
                        job.IsHighlight = true;
                        job.IsUrgent = true;
                        job.IsHot = true;

                        foreach (var jobService in job.JobServices)
                        {
                            jobService.IsActive = true;
                        }

                        context.Jobs.Add(job);
                        await context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return Ok(new { Message = "Create job successfully." });
                    }

                    var order = new Order
                    {
                        Id = Guid.NewGuid(),
                        UserId = recruiter.Id,
                        Amount = totalCash,
                        Gateway = request.Gateway,
                        Type = OrderType.Job,
                        TargetId = job.Id,
                        Created = DateTime.UtcNow,
                        Modified = DateTime.UtcNow
                    };

                    context.Orders.Add(order);
                    context.Jobs.Add(job);
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new { order.Id, Message = "Create job successfully, pay to active services." });
                }
            }

            context.Jobs.Add(job);

            if (request.JobDescriptionFile != null)
            {
                var jobDescriptionsFolder = Path.Combine(env.WebRootPath, "jobDescriptions");
                if (!Directory.Exists(jobDescriptionsFolder))
                {
                    Directory.CreateDirectory(jobDescriptionsFolder);
                }

                var cleanFileName = PathHelper.GetCleanFileName(request.JobDescriptionFile.FileName);
                var displayName = Path.GetFileName(cleanFileName);

                var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(request.JobDescriptionFile.FileName)}";
                var filePath = Path.Combine(jobDescriptionsFolder, uniqueFileName);

                await using var stream = new FileStream(filePath, FileMode.Create);
                await request.JobDescriptionFile.CopyToAsync(stream);

                var jobDescription = new AttachedFile
                {
                    Id = Guid.NewGuid(),
                    Name = displayName,
                    Path = PathHelper.GetRelativePathFromAbsolute(filePath, env.WebRootPath),
                    Type = FileType.JobDescription,
                    Uploaded = DateTime.UtcNow,
                    UploadedById = recruiterGuid
                };
                context.AttachedFiles.Add(jobDescription);
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { Message = "Create job successfully." });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { ex.Message });
        }
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "RecruiterVerifiedOnly")]
    public async Task<IActionResult> UpdateJob([FromRoute] Guid id, [FromForm] UpdateJobRequest request)
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

            // Handle Job Description File Upload
            if (request.JobDescriptionFile != null)
            {
                var allowedExtensions = new[] { ".pdf", ".docx", ".doc" };
                var extension = Path.GetExtension(request.JobDescriptionFile.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest(new { Message = "Invalid file type. Only PDF, DOCX and DOC are allowed." });
                }

                if (request.JobDescriptionFile.Length > 10 * 1024 * 1024)
                {
                    return BadRequest(new { Message = "File size exceeds 10MB limit." });
                }

                var jobDescriptionsFolder = Path.Combine(env.WebRootPath, "jobDescriptions");
                if (!Directory.Exists(jobDescriptionsFolder))
                {
                    Directory.CreateDirectory(jobDescriptionsFolder);
                }

                var cleanFileName = PathHelper.GetCleanFileName(request.JobDescriptionFile.FileName);
                var displayFileName = Path.GetFileName(cleanFileName);

                var jobDescriptionFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(jobDescriptionsFolder, jobDescriptionFileName);
                await using var stream = new FileStream(filePath, FileMode.Create);
                await request.JobDescriptionFile.CopyToAsync(stream);

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var parsedUserId))
                {
                    return Unauthorized(new { Message = "Invalid user identity." });
                }

                var jobDescription = new AttachedFile
                {
                    Id = Guid.NewGuid(),
                    Name = displayFileName,
                    Path = PathHelper.GetRelativePathFromAbsolute(filePath, env.WebRootPath),
                    Type = FileType.JobDescription,
                    TargetId = job.Id,
                    UploadedById = parsedUserId
                };
                context.AttachedFiles.Add(jobDescription);
                await context.SaveChangesAsync();
            }

            job.Status = JobStatus.Pending;
            job.Modified = DateTime.UtcNow;
            context.Jobs.Update(job);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok(new { Message = $"Update job {id} successfully" });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500,
                new { Message = "An error occurred while updating the job.", Errors = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/close")]
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "RecruiterVerifiedOnly")]
    public async Task<IActionResult> CloseJob(Guid id)
    {
        var job = await context.Jobs.SingleOrDefaultAsync(j => j.Id == id && j.Status == JobStatus.Active);
        if (job == null)
        {
            return NotFound(new { Message = "Job not found." });
        }

        job.Status = JobStatus.Closed;
        context.Jobs.Update(job);
        await context.SaveChangesAsync();
        return Ok(new { Message = "Close job successfully." });
    }

    [HttpPatch("{id:guid}/publish")]
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "RecruiterVerifiedOnly")]
    public async Task<IActionResult> Publish(Guid id)
    {
        var job = await context.Jobs.SingleOrDefaultAsync(j => j.Id == id && j.Status == JobStatus.Draft);
        if (job == null)
        {
            return NotFound(new { Message = "Job not found." });
        }

        job.Status = JobStatus.Pending;
        context.Jobs.Update(job);
        await context.SaveChangesAsync();
        return Ok(new { Message = "Publish job successfully. Need accept from Admin to active job." });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "RecruiterVerifiedOnly")]
    public async Task<IActionResult> DeleteJob(Guid id)
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
    public async Task<ActionResult<IEnumerable<ApplicationResponse>>> GetJobApplications(
        [FromRoute] Guid jobId,
        [FromQuery] string? search,
        [FromQuery] bool? seen,
        [FromQuery] ApplicationProcess? process,
        [FromQuery] bool? appointment,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
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
            .Include(a => a.Appointment)
            .Include(a => a.Job)
            .ThenInclude(j => j.Campaign)
            .Where(a => a.JobId == jobId && a.Status != ApplicationStatus.Draft)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(a =>
                a.Introduction != null &&
                (a.FullName.Contains(search) ||
                 a.Introduction.Contains(search) ||
                 a.PhoneNumber.Contains(search) ||
                 a.Email.Contains(search)));
        }

        if (seen.HasValue)
        {
            query = seen.Value
                ? query.Where(a => a.Status == ApplicationStatus.Seen)
                : query.Where(a => a.Status != ApplicationStatus.Seen);
        }

        if (process.HasValue)
        {
            query = query.Where(a => a.Process == process);
        }

        if (appointment.HasValue)
        {
            query = appointment.Value
                ? query.Where(a => a.Appointment != null)
                : query.Where(a => a.Appointment == null);
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var applications = await query
            .OrderByDescending(a => a.Created)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var applicationIds = applications.Select(a => a.Id).ToList();
        var resumes = await context.AttachedFiles
            .Where(f => f.Type == FileType.Application && applicationIds.Contains(f.TargetId))
            .Select(f => new FileResponse
            {
                Id = f.Id,
                TargetId = f.TargetId,
                Name = f.Name,
                Path = f.Path,
                Uploaded = f.Uploaded
            })
            .ToListAsync();

        var items = applications.Select(a => new ApplicationResponse
        {
            Id = a.Id,
            JobId = a.JobId,
            FullName = a.FullName,
            Email = a.Email,
            PhoneNumber = a.PhoneNumber,
            Introduction = a.Introduction,
            Resume = resumes.SingleOrDefault(r => r.TargetId == a.Id),
            Status = a.Status,
            Process = a.Process,
            Appointment = a.Appointment != null
                ? new AppointmentShortResponse
                {
                    Id = a.Appointment.Id,
                    StartTime = a.Appointment.StartTime,
                    EndTime = a.Appointment.EndTime,
                    Note = a.Appointment.Note,
                    Participant = null,
                    Created = a.Appointment.Created
                }
                : null,
            Submitted = a.Submitted,
            Created = a.Created,
            Modified = a.Modified
        }).ToList();

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
            .Include(a => a.Appointment)
            .SingleOrDefaultAsync(a =>
                a.Id == applicationId && a.JobId == jobId && a.Status != ApplicationStatus.Draft);

        if (application == null)
        {
            return NotFound(new { Message = "Application not found." });
        }

        var response = new ApplicationResponse
        {
            Id = application.Id,
            JobId = application.JobId,
            FullName = application.FullName,
            Email = application.Email,
            PhoneNumber = application.PhoneNumber,
            Introduction = application.Introduction,
            Resume = await context.AttachedFiles
                .Where(f => f.Type == FileType.Application && f.TargetId == application.Id)
                .Select(f => new FileResponse
                {
                    Id = f.Id,
                    TargetId = f.TargetId,
                    Name = f.Name,
                    Path = f.Path,
                    Uploaded = f.Uploaded
                })
                .SingleOrDefaultAsync(),
            Status = application.Status,
            Process = application.Process,
            Appointment = application.Appointment != null
                ? new AppointmentShortResponse
                {
                    Id = application.Appointment.Id,
                    StartTime = application.Appointment.StartTime,
                    EndTime = application.Appointment.EndTime,
                    Note = application.Appointment.Note,
                    Participant = null,
                    Created = application.Appointment.Created
                }
                : null,
            Submitted = application.Submitted,
            Created = application.Created,
            Modified = application.Modified
        };

        return Ok(response);
    }
}