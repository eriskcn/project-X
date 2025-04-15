using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Models;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/applications")]
[Authorize]
public class ApplicationController(ApplicationDbContext context) : ControllerBase
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
            .Where(a => a.CandidateId == Guid.Parse(userId))
            .AsNoTracking()
            .AsQueryable();

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var applications = await query
            .OrderByDescending(a => a.Created)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = await Task.WhenAll(applications.Select(async a =>
        {
            var recruiter = a.Job.Campaign.Recruiter;
            var freelanceRecruiterRoleId = await context.Roles
                .Where(r => r.Name == "FreelanceRecruiter")
                .Select(r => r.Id)
                .SingleOrDefaultAsync();

            var isFreelanceRecruiter = await context.UserRoles
                .AnyAsync(ur => ur.UserId == recruiter.Id && ur.RoleId == freelanceRecruiterRoleId);

            return new ApplicationResponseForCandidate
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
                Modified = a.Modified,
                Job = new JobResponseForCandidate
                {
                    Id = a.Job.Id,
                    Title = a.Job.Title,
                    Description = a.Job.Description,
                    OfficeAddress = a.Job.OfficeAddress,
                    Quantity = a.Job.Quantity,
                    Status = a.Job.Status,
                    EducationLevelRequire = a.Job.EducationLevelRequire,
                    YearOfExperience = a.Job.YearOfExperience,
                    MinSalary = a.Job.MinSalary,
                    MaxSalary = a.Job.MaxSalary,
                    IsHighlight = a.Job.IsHighlight,
                    HighlightStart = a.Job.HighlightStart,
                    HighlightEnd = a.Job.HighlightEnd,
                    Major = new MajorResponse { Id = a.Job.Major.Id, Name = a.Job.Major.Name },
                    Location = new LocationResponse { Id = a.Job.Location.Id, Name = a.Job.Location.Name },
                    JobDescription = await context.AttachedFiles
                        .Where(f => f.Type == TargetType.JobDescription && f.TargetId == a.Job.Id)
                        .Select(f => new FileResponse
                        {
                            Id = f.Id,
                            Name = f.Name,
                            Path = f.Path,
                            Uploaded = f.Uploaded
                        })
                        .SingleOrDefaultAsync(),
                    Skills = a.Job.Skills.Select(s => new SkillResponse
                        {
                            Id = s.Id,
                            Name = s.Name,
                        })
                        .ToList(),
                    ContractTypes = a.Job.ContractTypes.Select(ct => new ContractTypeResponse
                            { Id = ct.Id, Name = ct.Name })
                        .ToList(),
                    JobLevels = a.Job.JobLevels.Select(jl => new JobLevelResponse { Id = jl.Id, Name = jl.Name })
                        .ToList(),
                    JobTypes = a.Job.JobTypes.Select(jt => new JobTypeResponse { Id = jt.Id, Name = jt.Name }).ToList(),
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
                            Introduction = recruiter.CompanyDetail.Introduction
                        }
                        : new CompanyRecruiterResponse(),
                    Created = a.Job.Created,
                    Modified = a.Job.Modified
                }
            };
        }));

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
            .Where(f => f.Type == TargetType.Application && f.TargetId == application.Id)
            .Select(f => new FileResponse
            {
                Id = f.Id,
                Name = f.Name,
                Path = f.Path,
                Uploaded = f.Uploaded
            })
            .SingleOrDefaultAsync();

        var jobDescription = await context.AttachedFiles
            .Where(f => f.Type == TargetType.JobDescription && f.TargetId == application.Job.Id)
            .Select(f => new FileResponse
            {
                Id = f.Id,
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
            Applied = application.Created,
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
                HighlightStart = application.Job.HighlightStart,
                HighlightEnd = application.Job.HighlightEnd,
                Major = new MajorResponse
                {
                    Id = application.Job.Major.Id,
                    Name = application.Job.Major.Name
                },
                Location = new LocationResponse
                {
                    Id = application.Job.Location.Id,
                    Name = application.Job.Location.Name
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
                        Introduction = recruiter.CompanyDetail.Introduction
                    }
                    : new CompanyRecruiterResponse(),
                Created = application.Job.Created,
                Modified = application.Job.Modified
            }
        };

        return Ok(response);
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