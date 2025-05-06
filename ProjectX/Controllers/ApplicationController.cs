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
[Route("capablanca/api/v0/applications")]
[Authorize]
public class ApplicationController(ApplicationDbContext context, IWebHostEnvironment env) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Candidate")]
    public async Task<ActionResult<IEnumerable<ApplicationResponseForCandidate>>> GetOwnApplications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized(new { Message = "User ID not found in access token." });
        }

        var query = context.Applications
            .Include(a => a.Job)
            .ThenInclude(j => j.Major)
            .Include(a => a.Job)
            .ThenInclude(j => j.Location)
            .Include(a => a.Job)
            .ThenInclude(j => j.Campaign)
            .ThenInclude(c => c.Recruiter)
            .ThenInclude(r => r.CompanyDetail)
            .ThenInclude(cd => cd!.Majors)
            .Include(a => a.Job)
            .ThenInclude(j => j.Campaign)
            .ThenInclude(c => c.Recruiter)
            .ThenInclude(r => r.CompanyDetail)
            .ThenInclude(cd => cd!.Location)
            .Include(a => a.Job)
            .ThenInclude(j => j.Skills)
            .Include(a => a.Job)
            .ThenInclude(j => j.ContractTypes)
            .Include(a => a.Job)
            .ThenInclude(j => j.JobLevels)
            .Include(a => a.Job)
            .ThenInclude(j => j.JobTypes)
            .Where(a => a.CandidateId == Guid.Parse(userId))
            .AsNoTracking();

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        // Get paginated applications with all needed data
        var applications = await query
            .OrderByDescending(a => a.Created)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                Application = a,
                Resume = context.AttachedFiles
                    .Where(f => f.Type == FileType.Application)
                    .Select(f => new FileResponse
                    {
                        Id = f.Id,
                        TargetId = f.TargetId,
                        Name = f.Name,
                        Path = f.Path,
                        Uploaded = f.Uploaded
                    })
                    .SingleOrDefault(),
                JobDescription = context.AttachedFiles
                    .Where(f => f.Type == FileType.JobDescription)
                    .Select(f => new FileResponse
                    {
                        Id = f.Id,
                        TargetId = f.TargetId,
                        Name = f.Name,
                        Path = f.Path,
                        Uploaded = f.Uploaded
                    })
                    .SingleOrDefault()
            })
            .ToListAsync();

        // Get recruiter info in bulk
        var recruiterIds = applications.Select(a => a.Application.Job.Campaign.Recruiter.Id).Distinct().ToList();

        var freelanceRecruiterRoleId = await context.Roles
            .Where(r => r.Name == "FreelanceRecruiter")
            .Select(r => r.Id)
            .SingleOrDefaultAsync();

        var freelanceRecruiters = await context.UserRoles
            .Where(ur => ur.RoleId == freelanceRecruiterRoleId && recruiterIds.Contains(ur.UserId))
            .Select(ur => ur.UserId)
            .ToListAsync();

        // Build response
        var items = applications.Select(a =>
        {
            var app = a.Application;
            var recruiter = app.Job.Campaign.Recruiter;
            var isFreelanceRecruiter = freelanceRecruiters.Contains(recruiter.Id);

            return new ApplicationResponseForCandidate
            {
                Id = app.Id,
                FullName = app.FullName,
                Email = app.Email,
                PhoneNumber = app.PhoneNumber,
                Introduction = app.Introduction,
                Resume = a.Resume,
                Status = app.Status,
                Process = app.Process,
                Submitted = app.Submitted,
                Created = app.Created,
                Modified = app.Modified,
                Job = new JobResponseForCandidate
                {
                    Id = app.Job.Id,
                    Title = app.Job.Title,
                    Description = app.Job.Description,
                    OfficeAddress = app.Job.OfficeAddress,
                    Quantity = app.Job.Quantity,
                    Status = app.Job.Status,
                    EducationLevelRequire = app.Job.EducationLevelRequire,
                    YearOfExperience = app.Job.YearOfExperience,
                    MinSalary = app.Job.MinSalary,
                    MaxSalary = app.Job.MaxSalary,
                    IsHighlight = app.Job.IsHighlight,
                    IsHot = app.Job.IsHot,
                    IsUrgent = app.Job.IsUrgent,
                    StartDate = app.Job.StartDate,
                    EndDate = app.Job.EndDate,
                    Major = new MajorResponse
                    {
                        Id = app.Job.Major.Id,
                        Name = app.Job.Major.Name
                    },
                    Location = new LocationResponse
                    {
                        Id = app.Job.Location.Id,
                        Name = app.Job.Location.Name,
                        Region = app.Job.Location.Region
                    },
                    JobDescription = a.JobDescription,
                    Skills = app.Job.Skills.Select(s => new SkillResponse
                    {
                        Id = s.Id,
                        Name = s.Name,
                    }).ToList(),
                    ContractTypes = app.Job.ContractTypes.Select(ct => new ContractTypeResponse
                    {
                        Id = ct.Id,
                        Name = ct.Name
                    }).ToList(),
                    JobLevels = app.Job.JobLevels.Select(jl => new JobLevelResponse
                    {
                        Id = jl.Id,
                        Name = jl.Name
                    }).ToList(),
                    JobTypes = app.Job.JobTypes.Select(jt => new JobTypeResponse
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
                    Created = app.Job.Created,
                    Modified = app.Job.Modified
                }
            };
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

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Candidate")]
    public async Task<ActionResult<ApplicationResponseForCandidate>> GetOwnApplication(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized(new { Message = "User ID not found in access token." });
        }

        var application = await context.Applications
            .Include(a => a.Job)
            .ThenInclude(j => j.Major)
            .Include(a => a.Job)
            .ThenInclude(j => j.Location)
            .Include(a => a.Job)
            .ThenInclude(j => j.Campaign)
            .ThenInclude(c => c.Recruiter)
            .ThenInclude(r => r.CompanyDetail)
            .ThenInclude(cd => cd!.Majors)
            .Include(a => a.Job)
            .ThenInclude(j => j.Campaign)
            .ThenInclude(c => c.Recruiter)
            .ThenInclude(r => r.CompanyDetail)
            .ThenInclude(cd => cd!.Location)
            .Include(application => application.Job).ThenInclude(job => job.Skills)
            .Include(application => application.Job).ThenInclude(job => job.ContractTypes)
            .Include(application => application.Job).ThenInclude(job => job.JobLevels)
            .Include(application => application.Job).ThenInclude(job => job.JobTypes)
            .Where(a => a.CandidateId == Guid.Parse(userId) && a.Id == id)
            .AsNoTracking()
            .SingleOrDefaultAsync();

        if (application == null)
        {
            return NotFound(new { Message = "Application not found." });
        }

        var recruiter = application.Job.Campaign.Recruiter;
        var freelanceRecruiterRoleId = await context.Roles
            .Where(r => r.Name == "FreelanceRecruiter")
            .Select(r => r.Id)
            .SingleOrDefaultAsync();

        var isFreelanceRecruiter = await context.UserRoles
            .AnyAsync(ur => ur.UserId == recruiter.Id && ur.RoleId == freelanceRecruiterRoleId);

        var resume = await context.AttachedFiles
            .Where(f => f.Type == FileType.Application && f.TargetId == application.Id)
            .Select(f => new FileResponse
            {
                Id = f.Id,
                TargetId = f.TargetId,
                Name = f.Name,
                Path = f.Path,
                Uploaded = f.Uploaded
            })
            .SingleOrDefaultAsync();

        var jobDescription = await context.AttachedFiles
            .Where(f => f.Type == FileType.JobDescription && f.TargetId == application.Job.Id)
            .Select(f => new FileResponse
            {
                Id = f.Id,
                TargetId = f.TargetId,
                Name = f.Name,
                Path = f.Path,
                Uploaded = f.Uploaded
            })
            .SingleOrDefaultAsync();

        var response = new ApplicationResponseForCandidate
        {
            Id = application.Id,
            FullName = application.FullName,
            Email = application.Email,
            PhoneNumber = application.PhoneNumber,
            Introduction = application.Introduction,
            Resume = resume,
            Status = application.Status,
            Process = application.Process,
            Submitted = application.Submitted,
            Created = application.Created,
            Modified = application.Modified,
            Job = new JobResponseForCandidate
            {
                Id = application.Job.Id,
                Title = application.Job.Title,
                Description = application.Job.Description,
                OfficeAddress = application.Job.OfficeAddress,
                Quantity = application.Job.Quantity,
                Status = application.Job.Status,
                EducationLevelRequire = application.Job.EducationLevelRequire,
                YearOfExperience = application.Job.YearOfExperience,
                MinSalary = application.Job.MinSalary,
                MaxSalary = application.Job.MaxSalary,
                IsHighlight = application.Job.IsHighlight,
                IsHot = application.Job.IsHot,
                IsUrgent = application.Job.IsUrgent,
                StartDate = application.Job.StartDate,
                EndDate = application.Job.EndDate,
                Major = new MajorResponse
                {
                    Id = application.Job.Major.Id,
                    Name = application.Job.Major.Name
                },
                Location = new LocationResponse
                {
                    Id = application.Job.Location.Id,
                    Name = application.Job.Location.Name,
                    Region = application.Job.Location.Region
                },
                JobDescription = jobDescription,
                Skills = application.Job.Skills
                    .Select(s => new SkillResponse { Id = s.Id, Name = s.Name })
                    .ToList(),
                ContractTypes = application.Job.ContractTypes
                    .Select(ct => new ContractTypeResponse { Id = ct.Id, Name = ct.Name })
                    .ToList(),
                JobLevels = application.Job.JobLevels
                    .Select(jl => new JobLevelResponse { Id = jl.Id, Name = jl.Name })
                    .ToList(),
                JobTypes = application.Job.JobTypes
                    .Select(jt => new JobTypeResponse { Id = jt.Id, Name = jt.Name })
                    .ToList(),
                FreelanceRecruiter = isFreelanceRecruiter
                    ? new FreelanceRecruiterResponse
                    {
                        Id = recruiter.Id,
                        FullName = recruiter.FullName,
                        Email = recruiter.Email ?? string.Empty,
                        ProfilePicture = recruiter.ProfilePicture,
                        LinkedInProfile = recruiter.LinkedInProfile ?? string.Empty,
                        GitHubProfile = recruiter.GitHubProfile ?? string.Empty
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
                Created = application.Job.Created,
                Modified = application.Job.Modified
            }
        };

        return Ok(response);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> UpdateApplication([FromRoute] Guid id, [FromForm] UpdateApplicationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null || !Guid.TryParse(userId, out var userIdGuid))
        {
            return Unauthorized(new { Message = "Invalid user identifier." });
        }

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var application = await context.Applications.FindAsync(id);
            if (application == null)
            {
                return NotFound(new { Message = "Application not found." });
            }

            if (userIdGuid != application.CandidateId)
            {
                return Unauthorized(new { Message = "You are not authorized to update this application." });
            }

            if (application.Status != ApplicationStatus.Draft)
            {
                return BadRequest(new { Message = "You cannot update a submitted or seen application." });
            }

            // Update application fields
            application.FullName = request.FullName ?? application.FullName;
            application.Email = request.Email ?? application.Email;
            application.PhoneNumber = request.PhoneNumber ?? application.PhoneNumber;
            application.Introduction = request.Introduction ?? application.Introduction;
            application.Status = request.Status ?? application.Status;
            application.Modified = DateTime.UtcNow;
            application.Submitted =
                request.Status == ApplicationStatus.Submitted ? DateTime.UtcNow : application.Submitted;

            // Handle resume file replacement
            if (request.Resume != null)
            {
                // Validate file
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
                var fileExtension = Path.GetExtension(request.Resume.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { Message = "Only PDF and Word documents are allowed." });
                }

                if (request.Resume.Length > 10 * 1024 * 1024) // 10MB
                {
                    return BadRequest(new { Message = "File size cannot exceed 10MB." });
                }

                var existingResume = await context.AttachedFiles
                    .Where(f => f.Type == FileType.Application && f.TargetId == id)
                    .SingleOrDefaultAsync();

                if (existingResume != null)
                {
                    var existingFilePath = Path.Combine(env.WebRootPath, existingResume.Path);
                    if (System.IO.File.Exists(existingFilePath))
                    {
                        System.IO.File.Delete(existingFilePath);
                    }

                    context.AttachedFiles.Remove(existingResume);
                }

                var resumeFolder = Path.Combine(env.WebRootPath, "resumes");
                if (!Directory.Exists(resumeFolder))
                {
                    Directory.CreateDirectory(resumeFolder);
                }

                var resumeFileName = $"{Guid.NewGuid()}{fileExtension}";
                var resumeFilePath = Path.Combine(resumeFolder, resumeFileName);

                await using (var stream = new FileStream(resumeFilePath, FileMode.Create))
                {
                    await request.Resume.CopyToAsync(stream);
                }

                context.AttachedFiles.Add(new AttachedFile
                {
                    Name = resumeFileName,
                    Path = PathHelper.GetRelativePathFromAbsolute(resumeFilePath, env.WebRootPath),
                    Type = FileType.Application,
                    TargetId = id,
                    UploadedById = userIdGuid,
                    Uploaded = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { Message = "Application updated successfully." });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { Message = "An error occurred while updating the application." });
        }
    }

    [HttpPatch("{id:guid}/seen")]
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "RecruiterVerifiedOnly")]
    public async Task<IActionResult> SeenApplication(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized(new { Message = "User ID not found in access token." });
        }

        var application = await context.Applications
            .Include(a => a.Job)
            .ThenInclude(j => j.Campaign)
            .SingleOrDefaultAsync(a => a.Id == id);

        if (application == null)
        {
            return NotFound(new { Message = "Application not found." });
        }

        if (Guid.Parse(userId) != application.Job.Campaign.RecruiterId)
        {
            return Forbid("You are not authorized to see this application.");
        }

        application.Status = ApplicationStatus.Seen;
        await context.SaveChangesAsync();

        return Ok(new { Message = "Application seen." });
    }


    [HttpPatch("{id:guid}/shortlist")]
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "RecruiterVerifiedOnly")]
    public async Task<IActionResult> ShortlistApplication(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized(new { Message = "User ID not found in access token." });
        }

        var application = await context.Applications
            .Include(a => a.Job)
            .ThenInclude(j => j.Campaign)
            .SingleOrDefaultAsync(a => a.Id == id);

        if (application == null)
        {
            return NotFound(new { Message = "Application not found." });
        }

        if (Guid.Parse(userId) != application.Job.Campaign.RecruiterId)
        {
            return Forbid("You are not authorized to shortlist this application.");
        }

        application.Process = ApplicationProcess.Shortlisted;
        await context.SaveChangesAsync();

        return Ok(new { Message = "Application shortlisted." });
    }

    [HttpPatch("{id:guid}/reject")]
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "RecruiterVerifiedOnly")]
    public async Task<IActionResult> RejectApplication(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized(new { Message = "User ID not found in access token." });
        }

        var application = await context.Applications
            .Include(a => a.Job)
            .ThenInclude(j => j.Campaign)
            .SingleOrDefaultAsync(a => a.Id == id);

        if (application == null)
        {
            return NotFound(new { Message = "Application not found." });
        }

        if (Guid.Parse(userId) != application.Job.Campaign.RecruiterId)
        {
            return Forbid("You are not authorized to reject this application.");
        }

        application.Process = ApplicationProcess.Rejected;
        await context.SaveChangesAsync();

        return Ok(new { Message = "Application rejected." });
    }

    [HttpPatch("{id:guid}/interview")]
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "RecruiterVerifiedOnly")]
    public async Task<IActionResult> InterviewApplication(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized(new { Message = "User ID not found in access token." });
        }

        var application = await context.Applications
            .Include(a => a.Job)
            .ThenInclude(j => j.Campaign)
            .SingleOrDefaultAsync(a => a.Id == id);

        if (application == null)
        {
            return NotFound(new { Message = "Application not found." });
        }

        if (Guid.Parse(userId) != application.Job.Campaign.RecruiterId)
        {
            return Forbid("You are not authorized to interview this application.");
        }

        application.Process = ApplicationProcess.Interviewing;
        await context.SaveChangesAsync();

        return Ok(new { Message = "Application interviewing." });
    }

    [HttpPatch("{id:guid}/offer")]
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "RecruiterVerifiedOnly")]
    public async Task<IActionResult> OfferApplication(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized(new { Message = "User ID not found in access token." });
        }

        var application = await context.Applications
            .Include(a => a.Job)
            .ThenInclude(j => j.Campaign)
            .SingleOrDefaultAsync(a => a.Id == id);

        if (application == null)
        {
            return NotFound(new { Message = "Application not found." });
        }

        if (Guid.Parse(userId) != application.Job.Campaign.RecruiterId)
        {
            return Forbid("You are not authorized to offer this application.");
        }

        application.Process = ApplicationProcess.Offered;
        await context.SaveChangesAsync();

        return Ok(new { Message = "Application offered." });
    }

    [HttpPatch("{id:guid}/hire")]
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "RecruiterVerifiedOnly")]
    public async Task<IActionResult> HireApplication(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized(new { Message = "User ID not found in access token." });
        }

        var application = await context.Applications
            .Include(a => a.Job)
            .ThenInclude(j => j.Campaign)
            .SingleOrDefaultAsync(a => a.Id == id);

        if (application == null)
        {
            return NotFound(new { Message = "Application not found." });
        }

        if (Guid.Parse(userId) != application.Job.Campaign.RecruiterId)
        {
            return Forbid("You are not authorized to hire this application.");
        }

        application.Process = ApplicationProcess.Hired;
        await context.SaveChangesAsync();

        return Ok(new { Message = "Application hired." });
    }
}