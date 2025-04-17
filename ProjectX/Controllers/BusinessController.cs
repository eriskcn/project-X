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
[Authorize(Roles = "Business")]
[Route("capablanca/api/v0/business")]
public class BusinessController(ApplicationDbContext context, IWebHostEnvironment env) : ControllerBase
{
    [HttpGet("verifications")]
    public async Task<ActionResult<BusinessVerifyResponse>> GetOwnBusinessVerification()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized("Access token is invalid.");
        }

        var user = await context.Users
            .Include(u => u.CompanyDetail)
            .SingleOrDefaultAsync(u => u.Id == Guid.Parse(userId));

        if (user == null)
        {
            return NotFound("User not found.");
        }

        var companyDetail = await context.CompanyDetails
            .Include(cd => cd.Majors)
            .Include(cd => cd.Location)
            .SingleOrDefaultAsync(cd => cd.CompanyId == Guid.Parse(userId));

        if (companyDetail == null)
        {
            return NotFound("Business registration not found.");
        }

        var registrationFile = await context.AttachedFiles
            .Where(f => f.Type == TargetType.BusinessRegistration && f.TargetId == companyDetail.Id)
            .Select(f => new FileResponse
            {
                Id = f.Id,
                Name = f.Name,
                Path = f.Path,
                Uploaded = f.Uploaded
            })
            .SingleOrDefaultAsync();

        var response = new BusinessVerifyResponse
        {
            CompanyId = companyDetail.CompanyId,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            BusinessVerified = user.RecruiterVerified,
            Company = new CompanyDetailResponse
            {
                Id = companyDetail.Id,
                CompanyName = companyDetail.CompanyName,
                ShortName = companyDetail.ShortName,
                TaxCode = companyDetail.TaxCode,
                HeadQuarterAddress = companyDetail.HeadQuarterAddress,
                Logo = companyDetail.Logo,
                ContactEmail = companyDetail.ContactEmail,
                ContactPhone = companyDetail.ContactPhone,
                Website = companyDetail.Website,
                FoundedYear = companyDetail.FoundedYear,
                Size = companyDetail.Size,
                Introduction = companyDetail.Introduction,
                Status = companyDetail.Status,
                Location = new LocationResponse
                {
                    Id = companyDetail.Location.Id,
                    Name = companyDetail.Location.Name
                },
                Majors = companyDetail.Majors.Select(m => new MajorResponse
                {
                    Id = m.Id,
                    Name = m.Name
                }).ToList(),
                RegistrationFile = registrationFile
            },
        };

        return Ok(response);
    }

    [HttpPost("verifications")]
    public async Task<IActionResult> BusinessVerify([FromForm] BusinessVerifyRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized("Access token is invalid.");
        }

        var summited = await context.CompanyDetails
            .AnyAsync(c => c.CompanyId == Guid.Parse(userId));

        if (summited)
        {
            return BadRequest(new { Message = "You have already submitted a business registration." });
        }

        var user = await context.Users.FindAsync(Guid.Parse(userId));
        if (user == null)
        {
            return NotFound("User not found.");
        }

        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync();

            if (request.Logo.Length == 0)
            {
                return BadRequest("Logo file is required.");
            }

            var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var logoExtension = Path.GetExtension(request.Logo.FileName).ToLowerInvariant();
            if (!allowedImageExtensions.Contains(logoExtension))
            {
                return BadRequest("Invalid logo file extension. Only image files are allowed.");
            }

            if (request.Logo.Length > 5 * 1024 * 1024)
            {
                return BadRequest("Logo file size exceeds the 5MB limit.");
            }

            var logosFolder = Path.Combine(env.WebRootPath, "logos");
            if (!Directory.Exists(logosFolder))
            {
                Directory.CreateDirectory(logosFolder);
            }

            var logoFileName = $"{Guid.NewGuid()}{Path.GetExtension(request.Logo.FileName)}";
            var logoUrl = Path.Combine(logosFolder, logoFileName);

            try
            {
                await using var stream = new FileStream(logoUrl, FileMode.Create);
                await request.Logo.CopyToAsync(stream);
            }
            catch (IOException ex)
            {
                return StatusCode(500, $"Error saving logo file: {ex.Message}");
            }

            if (request.RegistrationFile.Length == 0)
            {
                return BadRequest("Registration file is required.");
            }

            var allowedDocExtensions = new[] { ".pdf", ".docx", ".doc", ".png", ".jpeg", ".jpg" };
            var registrationFileExtension = Path.GetExtension(request.RegistrationFile.FileName).ToLowerInvariant();
            if (!allowedDocExtensions.Contains(registrationFileExtension))
            {
                return BadRequest("Invalid registration file extension. Only .pdf, .docx, and .doc files are allowed.");
            }

            if (request.RegistrationFile.Length > 5 * 1024 * 1024)
            {
                return BadRequest("Registration file size exceeds the 5MB limit.");
            }

            var businessRegistrationsFolder = Path.Combine(env.WebRootPath, "businessRegistrations");
            if (!Directory.Exists(businessRegistrationsFolder))
            {
                Directory.CreateDirectory(businessRegistrationsFolder);
            }

            var registrationFileName = $"{Guid.NewGuid()}{Path.GetExtension(request.RegistrationFile.FileName)}";
            var registrationUrl = Path.Combine(businessRegistrationsFolder, registrationFileName);

            try
            {
                await using var stream = new FileStream(registrationUrl, FileMode.Create);
                await request.RegistrationFile.CopyToAsync(stream);
            }
            catch (IOException ex)
            {
                return StatusCode(500, $"Error saving registration file: {ex.Message}");
            }

            if (request.MajorIds.Count == 0)
            {
                return BadRequest("At least one Major is required.");
            }

            var majors = await context.Majors
                .Where(m => request.MajorIds.Contains(m.Id))
                .ToListAsync();

            if (majors.Count != request.MajorIds.Count)
            {
                return BadRequest("One or more Major IDs are invalid.");
            }

            var companyDetail = new CompanyDetail
            {
                Id = Guid.NewGuid(),
                CompanyName = request.CompanyName,
                ShortName = request.ShortName,
                TaxCode = request.TaxCode,
                HeadQuarterAddress = request.HeadQuarterAddress,
                Logo = PathHelper.GetRelativePathFromAbsolute(logoUrl, env.WebRootPath),
                ContactEmail = request.ContactEmail,
                ContactPhone = request.ContactPhone,
                Website = request.Website ?? string.Empty,
                FoundedYear = request.FoundedYear,
                Size = request.Size,
                Introduction = request.Introduction,
                Status = VerifyStatus.Pending,
                CompanyId = Guid.Parse(userId),
                LocationId = request.LocationId,
                Majors = majors
            };

            var registrationAttachedFile = new AttachedFile
            {
                Name = registrationFileName,
                Path = PathHelper.GetRelativePathFromAbsolute(registrationUrl, env.WebRootPath),
                Type = TargetType.BusinessRegistration,
                TargetId = companyDetail.Id,
                UploadedById = Guid.Parse(userId)
            };
            user.VerificationSubmitted = true;

            try
            {
                context.CompanyDetails.Add(companyDetail);
                context.AttachedFiles.Add(registrationAttachedFile);
                context.Entry(user).Property(u => u.VerificationSubmitted).IsModified = true;
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error saving to database: {ex.Message}");
            }

            return Ok(new { Message = "Submit business registration successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
        }
    }

    [HttpPatch("verifications")]
    public async Task<IActionResult> UpdateBusinessVerify(
        [FromBody] UpdateBusinessVerifyRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized("Access token is invalid.");
        }

        var companyDetail = await context.CompanyDetails
            .Include(cd => cd.Majors)
            .SingleOrDefaultAsync(cd => cd.CompanyId == Guid.Parse(userId));

        if (companyDetail == null)
        {
            return NotFound("Business registration not found.");
        }

        if (request.CompanyName != null)
        {
            companyDetail.CompanyName = request.CompanyName;
        }

        if (request.ShortName != null)
        {
            companyDetail.ShortName = request.ShortName;
        }

        if (request.HeadQuarterAddress != null)
        {
            companyDetail.HeadQuarterAddress = request.HeadQuarterAddress;
        }

        if (request.TaxCode != null)
        {
            companyDetail.TaxCode = request.TaxCode;
        }

        if (request.ContactEmail != null)
        {
            companyDetail.ContactEmail = request.ContactEmail;
        }

        if (request.ContactPhone != null)
        {
            companyDetail.ContactPhone = request.ContactPhone;
        }

        if (request.Website != null)
        {
            companyDetail.Website = request.Website;
        }

        if (request.FoundedYear.HasValue)
        {
            companyDetail.FoundedYear = request.FoundedYear.Value;
        }

        if (request.Size.HasValue)
        {
            companyDetail.Size = request.Size.Value;
        }

        if (request.Introduction != null)
        {
            companyDetail.Introduction = request.Introduction;
        }

        companyDetail.Status = VerifyStatus.Pending;

        if (request.LocationId.HasValue)
        {
            companyDetail.LocationId = request.LocationId.Value;
        }

        if (request.MajorIds is { Count: > 0 })
        {
            var majors = await context.Majors
                .Where(m => request.MajorIds.Contains(m.Id))
                .ToListAsync();

            if (majors.Count != request.MajorIds.Count)
            {
                return BadRequest("One or more Major IDs are invalid.");
            }

            companyDetail.Majors = majors;
        }

        if (request.Logo != null)
        {
            var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var logoExtension = Path.GetExtension(request.Logo.FileName).ToLowerInvariant();
            if (!allowedImageExtensions.Contains(logoExtension))
            {
                return BadRequest("Invalid logo file extension. Only image files are allowed.");
            }

            if (request.Logo.Length > 5 * 1024 * 1024)
            {
                return BadRequest("Logo file size exceeds the 5MB limit.");
            }

            var logosFolder = Path.Combine(env.WebRootPath, "logos");
            if (!Directory.Exists(logosFolder))
            {
                Directory.CreateDirectory(logosFolder);
            }

            var logoFileName = $"{Guid.NewGuid()}{Path.GetExtension(request.Logo.FileName)}";
            var logoUrl = Path.Combine(logosFolder, logoFileName);

            try
            {
                await using var stream = new FileStream(logoUrl, FileMode.Create);
                await request.Logo.CopyToAsync(stream);
            }
            catch (IOException ex)
            {
                return StatusCode(500, $"Error saving logo file: {ex.Message}");
            }

            companyDetail.Logo = PathHelper.GetRelativePathFromAbsolute(logoUrl, env.WebRootPath);
        }

        if (request.RegistrationFile != null)
        {
            var allowedDocExtensions = new[] { ".pdf", ".docx", ".doc", ".png", ".jpeg", ".jpg" };
            var registrationFileExtension = Path.GetExtension(request.RegistrationFile.FileName).ToLowerInvariant();
            if (!allowedDocExtensions.Contains(registrationFileExtension))
            {
                return BadRequest("Invalid registration file extension. Only .pdf, .docx, and .doc files are allowed.");
            }

            if (request.RegistrationFile.Length > 5 * 1024 * 1024)
            {
                return BadRequest("Registration file size exceeds the 5MB limit.");
            }

            var businessRegistrationsFolder = Path.Combine(env.WebRootPath, "businessRegistrations");
            if (!Directory.Exists(businessRegistrationsFolder))
            {
                Directory.CreateDirectory(businessRegistrationsFolder);
            }

            var registrationFileName = $"{Guid.NewGuid()}{Path.GetExtension(request.RegistrationFile.FileName)}";
            var registrationUrl = Path.Combine(businessRegistrationsFolder, registrationFileName);

            try
            {
                await using var stream = new FileStream(registrationUrl, FileMode.Create);
                await request.RegistrationFile.CopyToAsync(stream);
            }
            catch (IOException ex)
            {
                return StatusCode(500, $"Error saving registration file: {ex.Message}");
            }

            var registrationAttachedFile = await context.AttachedFiles
                .SingleOrDefaultAsync(af =>
                    af.TargetId == companyDetail.Id && af.Type == TargetType.BusinessRegistration);

            if (registrationAttachedFile != null)
            {
                registrationAttachedFile.Name = registrationFileName;
                registrationAttachedFile.Path =
                    PathHelper.GetRelativePathFromAbsolute(registrationUrl, env.WebRootPath);
                registrationAttachedFile.UploadedById = Guid.Parse(userId);
            }
            else
            {
                registrationAttachedFile = new AttachedFile
                {
                    Name = registrationFileName,
                    Path = PathHelper.GetRelativePathFromAbsolute(registrationUrl, env.WebRootPath),
                    Type = TargetType.BusinessRegistration,
                    TargetId = companyDetail.Id,
                    UploadedById = Guid.Parse(userId)
                };
                context.AttachedFiles.Add(registrationAttachedFile);
            }
        }

        try
        {
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error saving to database: {ex.Message}");
        }

        return Ok(new { Message = "Business registration updated successfully." });
    }
}