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
            .Where(a => a.CandidateId == Guid.Parse(userId)
                        && a.Job.Campaign.Recruiter.CompanyDetail != null)
            .AsNoTracking()
            .AsQueryable();

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var applications = await query
            .OrderByDescending(a => a.Created)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = applications.Select(a => new ApplicationResponseForCandidate
        {
            Id = a.Id,
            FullName = a.FullName,
            Email = a.Email,
            PhoneNumber = a.PhoneNumber,
            Introduction = a.Introduction,
            Resume = context.AttachedFiles
                .Where(f => f.Type == TargetType.Application && f.TargetId == a.Id)
                .Select(f => new FileResponse
                {
                    Id = f.Id,
                    Name = f.Name,
                    Path = f.Path,
                    Uploaded = f.Uploaded
                })
                .SingleOrDefault(),
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
                Major = new MajorResponse
                {
                    Id = a.Job.Major.Id,
                    Name = a.Job.Major.Name
                },
                Location = new LocationResponse
                {
                    Id = a.Job.Location.Id,
                    Name = a.Job.Location.Name
                },
                JobDescription = context.AttachedFiles
                    .Where(f => f.Type == TargetType.JobDescription && f.TargetId == a.Job.Id)
                    .Select(f => new FileResponse
                    {
                        Id = f.Id,
                        Name = f.Name,
                        Path = f.Path,
                        Uploaded = f.Uploaded
                    })
                    .SingleOrDefault(),
                Skills = a.Job.Skills.Select(s => new SkillResponse
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description
                }).ToList(),
                ContractTypes = a.Job.ContractTypes.Select(st => new ContractTypeResponse
                {
                    Id = st.Id,
                    Name = st.Name
                }).ToList(),
                JobLevels = a.Job.JobLevels.Select(jl => new JobLevelResponse
                {
                    Id = jl.Id,
                    Name = jl.Name
                }).ToList(),
                JobTypes = a.Job.JobTypes.Select(jt => new JobTypeResponse
                {
                    Id = jt.Id,
                    Name = jt.Name
                }).ToList(),
                Recruiter = new RecruiterResponse
                {
                    Id = a.Job.Campaign.Recruiter.Id,
                    CompanyName = a.Job.Campaign.Recruiter.CompanyDetail!.CompanyName,
                    HeadQuarterAddress = a.Job.Campaign.Recruiter.CompanyDetail.HeadQuarterAddress,
                    Logo = a.Job.Campaign.Recruiter.CompanyDetail.Logo,
                    ContactEmail = a.Job.Campaign.Recruiter.CompanyDetail.ContactEmail,
                    FoundedYear = a.Job.Campaign.Recruiter.CompanyDetail.FoundedYear,
                    Introduction = a.Job.Campaign.Recruiter.CompanyDetail.Introduction
                },
                Created = a.Job.Created,
                Modified = a.Job.Modified
            }
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

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Candidate")]
    public async Task<ActionResult<ApplicationResponse>> GetOwnApplication(Guid id)
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
            .Where(a => a.CandidateId == Guid.Parse(userId) && a.Id == id &&
                        a.Job.Campaign.Recruiter.CompanyDetail != null)
            .Select(a => new ApplicationResponseForCandidate
            {
                Id = a.Id,
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
                    Major = new MajorResponse
                    {
                        Id = a.Job.Major.Id,
                        Name = a.Job.Major.Name
                    },
                    Location = new LocationResponse
                    {
                        Id = a.Job.Location.Id,
                        Name = a.Job.Location.Name
                    },
                    JobDescription = context.AttachedFiles
                        .Where(f => f.Type == TargetType.JobDescription && f.TargetId == a.Job.Id)
                        .Select(f => new FileResponse
                        {
                            Id = f.Id,
                            Name = f.Name,
                            Path = f.Path,
                            Uploaded = f.Uploaded
                        })
                        .SingleOrDefault(),
                    Skills = a.Job.Skills.Select(s => new SkillResponse
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Description = s.Description
                    }).ToList(),
                    ContractTypes = a.Job.ContractTypes.Select(st => new ContractTypeResponse
                    {
                        Id = st.Id,
                        Name = st.Name
                    }).ToList(),
                    JobLevels = a.Job.JobLevels.Select(jl => new JobLevelResponse
                    {
                        Id = jl.Id,
                        Name = jl.Name
                    }).ToList(),
                    JobTypes = a.Job.JobTypes.Select(jt => new JobTypeResponse
                    {
                        Id = jt.Id,
                        Name = jt.Name
                    }).ToList(),
                    Recruiter = new RecruiterResponse
                    {
                        Id = a.Job.Campaign.Recruiter.Id,
                        CompanyName = a.Job.Campaign.Recruiter.CompanyDetail!.CompanyName,
                        HeadQuarterAddress = a.Job.Campaign.Recruiter.CompanyDetail.HeadQuarterAddress,
                        Logo = a.Job.Campaign.Recruiter.CompanyDetail.Logo,
                        ContactEmail = a.Job.Campaign.Recruiter.CompanyDetail.ContactEmail,
                        FoundedYear = a.Job.Campaign.Recruiter.CompanyDetail.FoundedYear,
                        Introduction = a.Job.Campaign.Recruiter.CompanyDetail.Introduction
                    },
                    Created = a.Job.Created,
                    Modified = a.Job.Modified
                },
                FullName = a.FullName,
                Email = a.Email,
                PhoneNumber = a.PhoneNumber,
                Resume = context.AttachedFiles
                    .Where(f => f.Type == TargetType.Application && f.TargetId == a.Id)
                    .Select(f => new FileResponse
                    {
                        Id = f.Id,
                        Name = f.Name,
                        Path = f.Path,
                        Uploaded = f.Uploaded
                    })
                    .SingleOrDefault(),
                Introduction = a.Introduction,
                Status = a.Status,
                Created = a.Created
            })
            .SingleOrDefaultAsync();

        if (application == null)
        {
            return NotFound(new { Message = "Application not found." });
        }

        return Ok(application);
    }

    [HttpPatch("{id:guid}/seen")]
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "BusinessVerifiedOnly")]
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
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "BusinessVerifiedOnly")]
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
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "BusinessVerifiedOnly")]
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
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "BusinessVerifiedOnly")]
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
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "BusinessVerifiedOnly")]
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
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "BusinessVerifiedOnly")]
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