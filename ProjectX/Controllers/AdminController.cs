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
    public async Task<ActionResult<IEnumerable<BusinessVerifyResponse>>> GetVerifications(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new { Message = "Page number and page size must be greater than zero." });
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
                    BusinessVerified = user.BusinessVerified,
                    Company = new CompanyDetailResponse
                    {
                        Id = company.Id,
                        CompanyName = company.CompanyName,
                        ShortName = company.ShortName,
                        HeadQuarterAddress = company.HeadQuarterAddress,
                        Logo = company.Logo,
                        ContactEmail = company.ContactEmail,
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
                            .FirstOrDefault()
                    }
                });

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(v => v.Company.CompanyName.Contains(search));
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


    [HttpGet("business-verifications/{id:guid}")]
    public async Task<ActionResult<BusinessVerifyResponse>> GetVerification(Guid id)
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
            .Where(u => u.Id == id)
            .Join(context.CompanyDetails,
                user => user.Id,
                company => company.CompanyId,
                (user, company) => new BusinessVerifyResponse
                {
                    CompanyId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email!,
                    PhoneNumber = user.PhoneNumber!,
                    BusinessVerified = user.BusinessVerified,
                    Company = new CompanyDetailResponse
                    {
                        Id = company.Id,
                        CompanyName = company.CompanyName,
                        ShortName = company.ShortName,
                        HeadQuarterAddress = company.HeadQuarterAddress,
                        Logo = company.Logo,
                        ContactEmail = company.ContactEmail,
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


    [HttpPatch("business-verifications/{id:guid}")]
    public async Task<IActionResult> VerifyBusiness(Guid id)
    {
        var user = await context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        user.BusinessVerified = true;
        await context.SaveChangesAsync();

        return Ok(new { Message = "Business verified." });
    }
}