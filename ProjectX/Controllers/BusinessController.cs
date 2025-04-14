using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Models;

namespace ProjectX.Controllers;

[ApiController]
[Authorize(Roles = "Business")]
[Route("capablanca/api/v0/business")]
public class BusinessController(ApplicationDbContext context, IWebHostEnvironment env) : ControllerBase
{
    [HttpPost("verify")]
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

            var allowedDocExtensions = new[] { ".pdf", ".docx", ".doc" };
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
            var registrationUrl = $"/businessRegistrations/{registrationFileName}";

            try
            {
                await using var stream = new FileStream(registrationUrl, FileMode.Create);
                await request.RegistrationFile.CopyToAsync(stream);
            }
            catch (IOException ex)
            {
                return StatusCode(500, $"Error saving registration file: {ex.Message}");
            }

            var companyDetail = new CompanyDetail
            {
                Id = Guid.NewGuid(),
                CompanyName = request.CompanyName,
                ShortName = request.ShortName,
                TaxCode = request.TaxCode,
                HeadQuarterAddress = request.HeadQuarterAddress,
                Logo = logoUrl,
                ContactEmail = request.ContactEmail,
                ContactPhone = request.ContactPhone,
                Website = request.Website ?? string.Empty,
                FoundedYear = request.FoundedYear,
                Size = request.Size,
                Introduction = request.Introduction,
                CompanyId = Guid.Parse(userId),
                LocationId = request.LocationId,
                MajorId = request.MajorId
            };

            var registrationAttachedFile = new AttachedFile
            {
                Name = registrationFileName,
                Path = registrationUrl,
                Type = TargetType.BusinessRegistration,
                TargetId = companyDetail.Id,
                UploadedById = Guid.Parse(userId)
            };

            try
            {
                context.CompanyDetails.Add(companyDetail);
                context.AttachedFiles.Add(registrationAttachedFile);
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
}