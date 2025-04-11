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
        [FromQuery] string? filter,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new
                { Message = "Page number and page size must be zero or greater." });
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
            .Join(context.CompanyDetails,
                user => user.Id,
                company => company.CompanyId,
                (user, company) => new BusinessVerifyResponse
                {
                    CompanyId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email!,
                    PhoneNumber = user.PhoneNumber!,
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
                        Location = new LocationResponse
                        {
                            Id = company.Location.Id,
                            Name = company.Location.Name
                        },
                        Major = new MajorResponse
                        {
                            Id = company.Major.Id,
                            Name = company.Major.Name
                        },
                        RegistrationFile = context.AttachedFiles
                            .Where(f => f.Type == TargetType.CompanyDetail && f.TargetId == company.Id)
                            .Select(f => new FileResponse
                            {
                                Id = f.Id,
                                Name = f.Name,
                                Path = f.Path,
                                Uploaded = f.Uploaded
                            })
                            .SingleOrDefault()
                    }
                });

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(v => v.Company.CompanyName.Contains(search));
        }

        if (filter == "unverified")
        {
            query = query.Where(v => !v.BusinessVerified);
        }

        var totalItems = await query.CountAsync();

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var verifications = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            TotalItems = totalItems,
            TotalPages = totalPages,
            PageNumber = page,
            PageSize = pageSize,
            Items = verifications
        });
    }


    [HttpGet("business-verifications/{userId:guid}")]
    public async Task<ActionResult<BusinessVerifyResponse>> GetBusinessVerification(Guid userId)
    {
        var businessRole = await roleManager.Roles.SingleOrDefaultAsync(r => r.Name == "Business");
        if (businessRole == null)
        {
            return BadRequest(new { Message = "Business role not found." });
        }

        var businessUserIds = await context.UserRoles
            .Where(ur => ur.RoleId == businessRole.Id)
            .Select(ur => ur.UserId)
            .ToListAsync();

        var verification = await context.Users
            .Where(u => businessUserIds.Contains(u.Id))
            .Where(u => u.Id == userId)
            .Join(context.CompanyDetails,
                user => user.Id,
                company => company.CompanyId,
                (user, company) => new BusinessVerifyResponse
                {
                    CompanyId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email!,
                    PhoneNumber = user.PhoneNumber!,
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
                        Location = new LocationResponse
                        {
                            Id = company.Location.Id,
                            Name = company.Location.Name
                        },
                        Major = new MajorResponse
                        {
                            Id = company.Major.Id,
                            Name = company.Major.Name
                        },
                        RegistrationFile = context.AttachedFiles
                            .Where(f => f.Type == TargetType.CompanyDetail && f.TargetId == company.Id)
                            .Select(f => new FileResponse
                            {
                                Id = f.Id,
                                Name = f.Name,
                                Path = f.Path,
                                Uploaded = f.Uploaded
                            })
                            .SingleOrDefault()
                    }
                })
            .SingleOrDefaultAsync();

        if (verification == null)
        {
            return NotFound(new { Message = "Verification not found." });
        }

        return Ok(verification);
    }


    [HttpPatch("business-verifications/{userId:guid}")]
    public async Task<IActionResult> VerifyBusiness(Guid userId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        user.RecruiterVerified = true;
        await context.SaveChangesAsync();

        return Ok(new { Message = "Business verified." });
    }

    [HttpGet("freelance-recruiter-verifications")]
    public async Task<ActionResult<IEnumerable<FreelanceRecruiterVerifyResponse>>> GetFreelanceRecruiterVerifications(
        [FromQuery] string? search,
        [FromQuery] string? filter,
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
                        FrontIdCard = context.AttachedFiles
                            .Where(f => f.Type == TargetType.FrontIdCard && f.TargetId == freelanceRecruiter.Id)
                            .Select(f => new FileResponse
                            {
                                Id = f.Id,
                                Name = f.Name,
                                Path = f.Path,
                                Uploaded = f.Uploaded
                            })
                            .SingleOrDefault(),
                        BackIdCard = context.AttachedFiles
                            .Where(f => f.Type == TargetType.BackIdCard && f.TargetId == freelanceRecruiter.Id)
                            .Select(f => new FileResponse
                            {
                                Id = f.Id,
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

        if (filter == "unverified")
        {
            query = query.Where(q => !q.FreelanceRecruiterVerified);
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
                        FrontIdCard = context.AttachedFiles
                            .Where(f => f.Type == TargetType.FrontIdCard && f.TargetId == freelanceRecruiter.Id)
                            .Select(f => new FileResponse
                            {
                                Id = f.Id,
                                Name = f.Name,
                                Path = f.Path,
                                Uploaded = f.Uploaded
                            })
                            .SingleOrDefault(),
                        BackIdCard = context.AttachedFiles
                            .Where(f => f.Type == TargetType.BackIdCard && f.TargetId == freelanceRecruiter.Id)
                            .Select(f => new FileResponse
                            {
                                Id = f.Id,
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

    public async Task<IActionResult> VerifyFreelanceRecruiter(Guid userId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        user.RecruiterVerified = true;
        await context.SaveChangesAsync();

        return Ok(new { Message = "Freelance recruiter verified." });
    }
}