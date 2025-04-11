using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Models;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/freelance-recruiter")]
[Authorize(Roles = "FreelanceRecruiter")]
public class FreelanceRecruiterController(ApplicationDbContext context, IWebHostEnvironment env) : ControllerBase
{
    [HttpPost("verify")]
    public async Task<IActionResult> FreelanceRecruiterVerify([FromForm] FreelanceRecruiterVerifyRequest request)
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
        var frontIdCardExtension = Path.GetExtension(request.FrontIdCard.FileName).ToLowerInvariant();
        var backIdCardExtension = Path.GetExtension(request.BackIdCard.FileName).ToLowerInvariant();
        if (!allowedImageExtensions.Contains(frontIdCardExtension) ||
            !allowedImageExtensions.Contains(backIdCardExtension))
        {
            return BadRequest("Invalid ID card file extension. Only image files are allowed.");
        }

        if (request.FrontIdCard.Length > 5 * 1024 * 1024 || request.BackIdCard.Length > 5 * 1024 * 1024)
        {
            return BadRequest("ID card file size exceeds the 5MB limit.");
        }

        var idCardsFolder = Path.Combine(env.WebRootPath, "idCards");
        if (!Directory.Exists(idCardsFolder))
        {
            Directory.CreateDirectory(idCardsFolder);
        }

        var frontIdCardFileName = $"{Guid.NewGuid()}{Path.GetExtension(request.FrontIdCard.FileName)}";
        var backIdCardFileName = $"{Guid.NewGuid()}{Path.GetExtension(request.BackIdCard.FileName)}";

        await using (var stream = new FileStream(Path.Combine(idCardsFolder, frontIdCardFileName), FileMode.Create))
        {
            await request.FrontIdCard.CopyToAsync(stream);
        }

        await using (var stream = new FileStream(Path.Combine(idCardsFolder, backIdCardFileName), FileMode.Create))
        {
            await request.BackIdCard.CopyToAsync(stream);
        }

        var frontUrl = $"/idCards/{frontIdCardFileName}";
        var backUrl = $"/idCards/{backIdCardFileName}";

        var freelanceRecruiterDetail = new FreelanceRecruiterDetail
        {
            Id = Guid.NewGuid(),
            FreelanceRecruiterId = user.Id
        };

        var frontIdCard = new AttachedFile
        {
            Name = frontIdCardFileName,
            Path = frontUrl,
            Type = TargetType.FrontIdCard,
            TargetId = freelanceRecruiterDetail.Id,
            UploadedById = user.Id
        };

        var backIdCard = new AttachedFile
        {
            Name = backIdCardFileName,
            Path = backUrl,
            Type = TargetType.BackIdCard,
            TargetId = freelanceRecruiterDetail.Id,
            UploadedById = user.Id
        };

        context.FreelanceRecruiterDetails.Add(freelanceRecruiterDetail);
        context.AttachedFiles.Add(frontIdCard);
        context.AttachedFiles.Add(backIdCard);
        await context.SaveChangesAsync();

        return Ok(new { Message = "Submit freelance recruiter registration successfully" });
    }
}