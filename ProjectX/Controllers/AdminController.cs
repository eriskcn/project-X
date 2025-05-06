using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Models;
using Microsoft.AspNetCore.Identity;

namespace ProjectX.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("capablanca/api/v0/admin")]
public class AdminController(
    ApplicationDbContext context,
    RoleManager<Role> roleManager)
    : ControllerBase
{
    [HttpGet("business-verifications")]
    public async Task<ActionResult<IEnumerable<BusinessVerifyResponse>>> GetBusinessVerifications(
        [FromQuery] string? search,
        [FromQuery] VerifyStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new
            {
                Message = "Page number and page size must be greater than zero."
            });
        }

        var businessRole = await roleManager.Roles.SingleOrDefaultAsync(r => r.Name == "Business");
        if (businessRole == null)
        {
            return BadRequest(new { Message = "Business role not found." });
        }

        var businessUserIds = await context.UserRoles
            .Where(ur => ur.RoleId == businessRole.Id)
            .Select(ur => ur.UserId)
            .ToListAsync();

        var query = context.Users
            .Where(u => businessUserIds.Contains(u.Id))
            .Include(u => u.CompanyDetail)
            .ThenInclude(cd => cd!.Location)
            .Where(u => u.CompanyDetail != null);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u => u.CompanyDetail!.CompanyName.Contains(search));
        }

        if (status.HasValue)
        {
            query = query.Where(u => u.CompanyDetail!.Status == status.Value);
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var paginatedUsers = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var results = new List<BusinessVerifyResponse>();

        foreach (var user in paginatedUsers)
        {
            var company = user.CompanyDetail!;

            var majors = await context.Majors
                .Where(m => m.Companies.Any(c => c.Id == company.Id))
                .Select(m => new MajorResponse
                {
                    Id = m.Id,
                    Name = m.Name
                })
                .ToListAsync();

            var registrationFile = await context.AttachedFiles
                .Where(f => f.Type == FileType.BusinessRegistration && f.TargetId == company.Id)
                .Select(f => new FileResponse
                {
                    Id = f.Id,
                    TargetId = f.TargetId,
                    Name = f.Name,
                    Path = f.Path,
                    Uploaded = f.Uploaded
                })
                .SingleOrDefaultAsync();

            results.Add(new BusinessVerifyResponse
            {
                CompanyId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                BusinessVerified = user.RecruiterVerified,
                Company = new CompanyDetailResponse
                {
                    Id = company.Id,
                    CompanyName = company.CompanyName,
                    ShortName = company.ShortName,
                    TaxCode = company.TaxCode,
                    HeadQuarterAddress = company.HeadQuarterAddress,
                    Logo = company.Logo,
                    ContactEmail = company.ContactEmail,
                    ContactPhone = company.ContactPhone,
                    Website = company.Website ?? string.Empty,
                    FoundedYear = company.FoundedYear,
                    Size = company.Size,
                    Introduction = company.Introduction,
                    Status = company.Status,
                    RejectReason = company.RejectReason,
                    Location = new LocationResponse
                    {
                        Id = company.Location.Id,
                        Name = company.Location.Name,
                        Region = company.Location.Region
                    },
                    Majors = majors,
                    RegistrationFile = registrationFile
                }
            });
        }

        return Ok(new
        {
            TotalItems = totalItems,
            TotalPages = totalPages,
            First = page == 1,
            Last = page == totalPages,
            PageNumber = page,
            PageSize = pageSize,
            Items = results
        });
    }


    [HttpGet("business-verifications/{userId:guid}")]
    public async Task<ActionResult<BusinessVerifyResponse>> GetBusinessVerification(Guid userId)
    {
        var userWithRole = await context.UserRoles
            .Join(roleManager.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new { UserRole = ur, Role = r })
            .Where(x => x.Role.Name == "Business" && x.UserRole.UserId == userId)
            .Select(x => x.UserRole.UserId)
            .SingleOrDefaultAsync();

        if (userWithRole == default)
        {
            return NotFound(new { Message = "User not found or is not a business user." });
        }

        var user = await context.Users
            .SingleOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        var company = await context.CompanyDetails
            .Include(c => c.Location)
            .SingleOrDefaultAsync(c => c.CompanyId == userId);

        if (company == null)
        {
            return NotFound(new { Message = "Company details not found." });
        }

        var majors = await context.Majors
            .Where(m => m.Companies.Any(c => c.Id == company.Id))
            .Select(m => new MajorResponse
            {
                Id = m.Id,
                Name = m.Name
            })
            .ToListAsync();

        var registrationFile = await context.AttachedFiles
            .Where(f => f.Type == FileType.BusinessRegistration && f.TargetId == company.Id)
            .Select(f => new FileResponse
            {
                Id = f.Id,
                TargetId = f.TargetId,
                Name = f.Name,
                Path = f.Path,
                Uploaded = f.Uploaded
            })
            .SingleOrDefaultAsync();

        var verification = new BusinessVerifyResponse
        {
            CompanyId = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            BusinessVerified = user.RecruiterVerified,
            Company = new CompanyDetailResponse
            {
                Id = company.Id,
                CompanyName = company.CompanyName,
                ShortName = company.ShortName,
                TaxCode = company.TaxCode,
                HeadQuarterAddress = company.HeadQuarterAddress,
                Logo = company.Logo,
                ContactEmail = company.ContactEmail,
                ContactPhone = company.ContactPhone,
                Website = company.Website,
                FoundedYear = company.FoundedYear,
                Size = company.Size,
                Introduction = company.Introduction,
                Status = company.Status,
                RejectReason = company.RejectReason,
                Location = new LocationResponse
                {
                    Id = company.Location.Id,
                    Name = company.Location.Name,
                    Region = company.Location.Region
                },
                Majors = majors,
                RegistrationFile = registrationFile
            }
        };

        return Ok(verification);
    }

    [HttpPatch("business-verifications/{userId:guid}/accept")]
    public async Task<IActionResult> VerifyBusiness(Guid userId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        var companyDetail = await context.CompanyDetails
            .SingleOrDefaultAsync(c => c.CompanyId == userId && c.Status == VerifyStatus.Pending);

        if (companyDetail == null)
        {
            return NotFound(new { Message = "Company detail not found or already verified/rejected." });
        }

        user.RecruiterVerified = true;
        companyDetail.Status = VerifyStatus.Verified;
        companyDetail.RejectReason = null;
        await context.SaveChangesAsync();

        return Ok(new { Message = "Business verified." });
    }

    [HttpPatch("business-verifications/{userId:guid}/reject")]
    public async Task<ActionResult<BusinessVerifyResponse>> RejectBusiness([FromRoute] Guid userId,
        [FromBody] RejectRequest request)
    {
        var user = await context.Users
            .Include(u => u.CompanyDetail)
            .Where(u => u.Id == userId && u.CompanyDetail != null && u.CompanyDetail.Status == VerifyStatus.Pending)
            .SingleOrDefaultAsync();

        if (user == null)
        {
            return NotFound(new { Message = "User not found or verified/rejected" });
        }

        user.RecruiterVerified = false;
        user.CompanyDetail!.Status = VerifyStatus.Rejected;
        user.CompanyDetail.RejectReason = request.RejectReason;
        await context.SaveChangesAsync();

        return Ok(new BusinessVerifyResponse
        {
            CompanyId = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            BusinessVerified = user.RecruiterVerified,
            Company = new CompanyDetailResponse
            {
                Id = user.CompanyDetail.Id,
                CompanyName = user.CompanyDetail.CompanyName,
                ShortName = user.CompanyDetail.ShortName,
                TaxCode = user.CompanyDetail.TaxCode,
                HeadQuarterAddress = user.CompanyDetail.HeadQuarterAddress,
                Logo = user.CompanyDetail.Logo,
                ContactEmail = user.CompanyDetail.ContactEmail,
                ContactPhone = user.CompanyDetail.ContactPhone,
                Website = user.CompanyDetail.Website ?? string.Empty,
                FoundedYear = user.CompanyDetail.FoundedYear,
                Size = user.CompanyDetail.Size,
                Introduction = user.CompanyDetail.Introduction,
                Status = user.CompanyDetail.Status,
                RejectReason = user.CompanyDetail.RejectReason
            }
        });
    }

    [HttpGet("freelance-recruiter-verifications")]
    public async Task<ActionResult<IEnumerable<FreelanceRecruiterVerifyResponse>>> GetFreelanceRecruiterVerifications(
        [FromQuery] string? search,
        [FromQuery] VerifyStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new
                { Message = "Page number and page size must be zero or greater." });
        }

        var freelanceRecruiterRole = await roleManager.Roles.SingleOrDefaultAsync(r => r.Name == "FreelanceRecruiter");
        if (freelanceRecruiterRole == null)
        {
            return BadRequest(new { Message = "Freelance recruiter role not found." });
        }

        var freelanceRecruiterUserIds = await context.UserRoles
            .Where(ur => ur.RoleId == freelanceRecruiterRole.Id)
            .Select(ur => ur.UserId)
            .ToListAsync();

        var query = context.Users
            .Where(u => freelanceRecruiterUserIds.Contains(u.Id))
            .Join(context.FreelanceRecruiterDetails,
                user => user.Id,
                freelanceRecruiter => freelanceRecruiter.FreelanceRecruiterId,
                (user, freelanceRecruiter) => new FreelanceRecruiterVerifyResponse
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email!,
                    PhoneNumber = user.PhoneNumber!,
                    ProfilePicture = user.ProfilePicture,
                    GitHubProfile = user.GitHubProfile,
                    LinkedInProfile = user.LinkedInProfile,
                    FreelanceRecruiter = new FreelanceRecruiterDetailResponse
                    {
                        Id = freelanceRecruiter.Id,
                        Status = freelanceRecruiter.Status,
                        RejectReason = freelanceRecruiter.RejectReason,
                        FrontIdCard = context.AttachedFiles
                            .Where(f => f.Type == FileType.FrontIdCard && f.TargetId == freelanceRecruiter.Id)
                            .Select(f => new FileResponse
                            {
                                Id = f.Id,
                                TargetId = f.TargetId,
                                Name = f.Name,
                                Path = f.Path,
                                Uploaded = f.Uploaded
                            })
                            .SingleOrDefault(),
                        BackIdCard = context.AttachedFiles
                            .Where(f => f.Type == FileType.BackIdCard && f.TargetId == freelanceRecruiter.Id)
                            .Select(f => new FileResponse
                            {
                                Id = f.Id,
                                TargetId = f.TargetId,
                                Name = f.Name,
                                Path = f.Path,
                                Uploaded = f.Uploaded
                            })
                            .SingleOrDefault()
                    },
                    FreelanceRecruiterVerified = user.RecruiterVerified
                });

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(q => q.FullName.Contains(search));
        }

        if (status.HasValue)
        {
            query = query.Where(q => q.FreelanceRecruiter.Status == status.Value);
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var freelanceRecruiterVerifications = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            TotalItems = totalItems,
            TotalPages = totalPages,
            PageNumber = page,
            PageSize = pageSize,
            Items = freelanceRecruiterVerifications
        });
    }

    [HttpGet("freelance-recruiter-verifications/{userId:guid}")]
    public async Task<ActionResult<FreelanceRecruiterVerifyResponse>> GetFreelanceRecruiterVerification(Guid userId)
    {
        var freelanceRecruiterRole = await roleManager.Roles.SingleOrDefaultAsync(r => r.Name == "FreelanceRecruiter");
        if (freelanceRecruiterRole == null)
        {
            return BadRequest(new { Message = "Freelance recruiter role not found." });
        }

        var freelanceRecruiterUserIds = await context.UserRoles
            .Where(ur => ur.RoleId == freelanceRecruiterRole.Id)
            .Select(ur => ur.UserId)
            .ToListAsync();

        var verification = context.Users
            .Where(u => freelanceRecruiterUserIds.Contains(u.Id))
            .Where(u => u.Id == userId)
            .Join(context.FreelanceRecruiterDetails,
                user => user.Id,
                freelanceRecruiter => freelanceRecruiter.FreelanceRecruiterId,
                (user, freelanceRecruiter) => new FreelanceRecruiterVerifyResponse
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email!,
                    PhoneNumber = user.PhoneNumber!,
                    ProfilePicture = user.ProfilePicture,
                    GitHubProfile = user.GitHubProfile,
                    LinkedInProfile = user.LinkedInProfile,
                    FreelanceRecruiter = new FreelanceRecruiterDetailResponse
                    {
                        Id = freelanceRecruiter.Id,
                        Status = freelanceRecruiter.Status,
                        RejectReason = freelanceRecruiter.RejectReason,
                        FrontIdCard = context.AttachedFiles
                            .Where(f => f.Type == FileType.FrontIdCard && f.TargetId == freelanceRecruiter.Id)
                            .Select(f => new FileResponse
                            {
                                Id = f.Id,
                                TargetId = f.TargetId,
                                Name = f.Name,
                                Path = f.Path,
                                Uploaded = f.Uploaded
                            })
                            .SingleOrDefault(),
                        BackIdCard = context.AttachedFiles
                            .Where(f => f.Type == FileType.BackIdCard && f.TargetId == freelanceRecruiter.Id)
                            .Select(f => new FileResponse
                            {
                                Id = f.Id,
                                TargetId = f.TargetId,
                                Name = f.Name,
                                Path = f.Path,
                                Uploaded = f.Uploaded
                            })
                            .SingleOrDefault()
                    },
                    FreelanceRecruiterVerified = user.RecruiterVerified
                });
        return Ok(verification);
    }

    [HttpPatch("freelance-recruiter-verifications/{userId:guid}/accept")]
    public async Task<IActionResult> VerifyFreelanceRecruiter(Guid userId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        var freelanceRecruiterDetail = await context.FreelanceRecruiterDetails
            .SingleOrDefaultAsync(f => f.FreelanceRecruiterId == userId && f.Status == VerifyStatus.Pending);

        if (freelanceRecruiterDetail == null)
        {
            return NotFound(new { Message = "Freelance recruiter detail not found or already verified/rejected." });
        }

        user.RecruiterVerified = true;
        freelanceRecruiterDetail.Status = VerifyStatus.Verified;
        freelanceRecruiterDetail.RejectReason = null;
        await context.SaveChangesAsync();

        return Ok(new { Message = "Freelance recruiter verified." });
    }

    [HttpPatch("freelance-recruiter-verifications/{userId:guid}/reject")]
    public async Task<IActionResult> RejectFreelanceRecruiter([FromRoute] Guid userId, [FromBody] RejectRequest request)
    {
        var user = await context.Users
            .Include(u => u.FreelanceRecruiterDetail)
            .Where(u => u.Id == userId && u.FreelanceRecruiterDetail != null &&
                        u.FreelanceRecruiterDetail.Status == VerifyStatus.Pending)
            .SingleOrDefaultAsync();

        if (user == null)
        {
            return NotFound(new { Message = "User not found or verified/rejected" });
        }

        user.RecruiterVerified = false;
        user.FreelanceRecruiterDetail!.Status = VerifyStatus.Rejected;
        user.FreelanceRecruiterDetail!.RejectReason = request.RejectReason;
        await context.SaveChangesAsync();

        return Ok(new FreelanceRecruiterVerifyResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            ProfilePicture = user.ProfilePicture,
            GitHubProfile = user.GitHubProfile,
            LinkedInProfile = user.LinkedInProfile,
            FreelanceRecruiter = new FreelanceRecruiterDetailResponse
            {
                Id = user.FreelanceRecruiterDetail.Id,
                Status = user.FreelanceRecruiterDetail.Status,
                RejectReason = user.FreelanceRecruiterDetail.RejectReason,
                FrontIdCard = context.AttachedFiles
                    .Where(f => f.Type == FileType.FrontIdCard && f.TargetId == user.FreelanceRecruiterDetail.Id)
                    .Select(f => new FileResponse
                    {
                        Id = f.Id,
                        TargetId = f.TargetId,
                        Name = f.Name,
                        Path = f.Path,
                        Uploaded = f.Uploaded
                    })
                    .SingleOrDefault(),
                BackIdCard = context.AttachedFiles
                    .Where(f => f.Type == FileType.BackIdCard && f.TargetId == user.FreelanceRecruiterDetail.Id)
                    .Select(f => new FileResponse
                    {
                        Id = f.Id,
                        TargetId = f.TargetId,
                        Name = f.Name,
                        Path = f.Path,
                        Uploaded = f.Uploaded
                    })
                    .SingleOrDefault()
            },
            FreelanceRecruiterVerified = user.RecruiterVerified
        });
    }

    [HttpGet("jobs")]
    public async Task<ActionResult<IEnumerable<JobResponse>>> GetPendingJobs(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new
            {
                Message = "Page number and page size must be greater than zero."
            });
        }

        var query = context.Jobs
            .Include(j => j.Major)
            .Include(j => j.Location)
            .Include(j => j.Campaign)
            .Include(j => j.ContractTypes)
            .Include(j => j.JobLevels)
            .Include(j => j.JobTypes)
            .Include(j => j.Skills)
            .Where(j => j.Status == JobStatus.Pending);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(j => j.Title.Contains(search));
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var paginatedJobs = await query
            .OrderByDescending(j => j.Created)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var results = paginatedJobs.Select(job => new JobResponse
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
            JobDescription = context.AttachedFiles
                .Where(f => f.Type == FileType.JobDescription && f.TargetId == job.Id)
                .Select(f => new FileResponse
                {
                    Id = f.Id,
                    Name = f.Name,
                    Path = f.Path,
                    TargetId = f.TargetId,
                    Uploaded = f.Uploaded
                })
                .SingleOrDefault(),
            Skills = job.Skills.Select(s => new SkillResponse
            {
                Id = s.Id,
                Name = s.Name
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
        }).ToList();

        return Ok(new
        {
            TotalItems = totalItems,
            TotalPages = totalPages,
            First = page == 1,
            Last = page == totalPages,
            PageNumber = page,
            PageSize = pageSize,
            Items = results
        });
    }

    [HttpGet("jobs/{id:guid}")]
    public async Task<ActionResult<JobResponse>> GetPendingJob(Guid id)
    {
        var job = await context.Jobs
            .Include(j => j.Major)
            .Include(j => j.Location)
            .Include(j => j.Campaign)
            .Include(j => j.ContractTypes)
            .Include(j => j.JobLevels)
            .Include(j => j.JobTypes)
            .Include(j => j.Skills)
            .SingleOrDefaultAsync(j => j.Id == id && j.Status == JobStatus.Pending);

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
            JobDescription = context.AttachedFiles
                .Where(f => f.Type == FileType.JobDescription && f.TargetId == job.Id)
                .Select(f => new FileResponse
                {
                    Id = f.Id,
                    Name = f.Name,
                    Path = f.Path,
                    TargetId = f.TargetId,
                    Uploaded = f.Uploaded
                })
                .SingleOrDefault(),
            Skills = job.Skills.Select(s => new SkillResponse
            {
                Id = s.Id,
                Name = s.Name
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

    [HttpPatch("jobs/{id:guid}/accept")]
    public async Task<IActionResult> AcceptJob([FromRoute] Guid id)
    {
        var job = await context.Jobs.FindAsync(id);
        if (job == null)
        {
            return NotFound(new { Message = "Job not found." });
        }

        if (job.Status != JobStatus.Pending)
        {
            return Conflict(new { Message = "Invalid job to accept." });
        }

        job.RejectReason = null;
        job.Status = JobStatus.Active;
        context.Jobs.Update(job);
        await context.SaveChangesAsync();

        return Ok(new { Message = $"Accept job {id} successfully." });
    }

    [HttpPatch("jobs/{id:guid}/reject")]
    public async Task<IActionResult> RejectJob([FromRoute] Guid id, [FromBody] RejectRequest request)
    {
        var job = await context.Jobs.FindAsync(id);
        if (job == null)
        {
            return NotFound(new { Message = "Job not found." });
        }

        if (job.Status != JobStatus.Pending)
        {
            return Conflict(new { Message = "Invalid job to reject." });
        }

        job.Status = JobStatus.Rejected;
        job.RejectReason = request.RejectReason;
        context.Jobs.Update(job);
        await context.SaveChangesAsync();

        return Ok(new { Message = $"Reject job {id} successfully." });
    }
}