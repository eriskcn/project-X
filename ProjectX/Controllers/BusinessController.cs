using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    [Authorize(Roles = "Business")]
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

        var user = await context.Users.FindAsync(Guid.Parse(userId));
        if (user == null)
        {
            return NotFound("User not found.");
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

        await using (var stream = new FileStream(Path.Combine(logosFolder, logoFileName), FileMode.Create))
        {
            await request.Logo.CopyToAsync(stream);
        }

        var logoUrl = $"/logos/{logoFileName}";

        var uploadsFolder = Path.Combine(env.WebRootPath, "uploads");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
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

        var registrationFileName = $"{Guid.NewGuid()}{Path.GetExtension(request.RegistrationFile.FileName)}";
        await using (var stream = new FileStream(Path.Combine(uploadsFolder, registrationFileName), FileMode.Create))
        {
            await request.RegistrationFile.CopyToAsync(stream);
        }

        var registrationUrl = $"/uploads/{registrationFileName}";

        var companyDetail = new CompanyDetail
        {
            Id = Guid.NewGuid(),
            CompanyName = request.CompanyName,
            ShortName = request.ShortName ?? string.Empty,
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
            Type = TargetType.CompanyDetail,
            TargetId = companyDetail.Id,
            UploadedById = Guid.Parse(userId)
        };

        context.CompanyDetails.Add(companyDetail);
        context.AttachedFiles.Add(registrationAttachedFile);
        await context.SaveChangesAsync();

        return Ok(new { Message = "Submit business registration successfully." });
    }
}