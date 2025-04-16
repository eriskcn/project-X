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
[Route("capablanca/api/v0/freelance-recruiter")]
[Authorize(Roles = "FreelanceRecruiter")]
public class FreelanceRecruiterController(ApplicationDbContext context, IWebHostEnvironment env) : ControllerBase
{
    [HttpGet("verifications")]
    public async Task<ActionResult<FreelanceRecruiterVerifyResponse>> GetOwnFreelanceRecruiterDetail()
    {
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

        var detail = await context.FreelanceRecruiterDetails
            .SingleOrDefaultAsync(x => x.FreelanceRecruiterId == user.Id);

        if (detail == null)
        {
            return NotFound("Freelance recruiter detail not found.");
        }

        return Ok(new FreelanceRecruiterVerifyResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            ProfilePicture = user.ProfilePicture,
            GitHubProfile = user.GitHubProfile,
            LinkedInProfile = user.LinkedInProfile,
            FreelanceRecruiter = new FreelanceRecruiterDetailResponse
            {
                Id = detail.Id,
                Status = detail.Status,
                FrontIdCard = await context.AttachedFiles
                    .Where(f => f.TargetId == detail.Id && f.Type == TargetType.FrontIdCard)
                    .Select(f => new FileResponse
                    {
                        Id = f.Id,
                        Name = f.Name,
                        Path = f.Path,
                        Uploaded = f.Uploaded
                    })
                    .SingleOrDefaultAsync(),
                BackIdCard = await context.AttachedFiles
                    .Where(f => f.TargetId == detail.Id && f.Type == TargetType.BackIdCard)
                    .Select(f => new FileResponse
                    {
                        Id = f.Id,
                        Name = f.Name,
                        Path = f.Path,
                        Uploaded = f.Uploaded
                    })
                    .SingleOrDefaultAsync()
            }
        });
    }

    [HttpPost("verifications")]
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

        var frontUrl = Path.Combine(idCardsFolder, frontIdCardFileName);
        var backUrl = Path.Combine(idCardsFolder, backIdCardFileName);

        var freelanceRecruiterDetail = new FreelanceRecruiterDetail
        {
            Id = Guid.NewGuid(),
            FreelanceRecruiterId = user.Id,
            Status = VerifyStatus.Pending
        };

        var frontIdCard = new AttachedFile
        {
            Name = frontIdCardFileName,
            Path = PathHelper.GetRelativePathFromAbsolute(frontUrl, env.WebRootPath),
            Type = TargetType.FrontIdCard,
            TargetId = freelanceRecruiterDetail.Id,
            UploadedById = user.Id
        };

        var backIdCard = new AttachedFile
        {
            Name = backIdCardFileName,
            Path = PathHelper.GetRelativePathFromAbsolute(backUrl, env.WebRootPath),
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

    [HttpPatch("verifications")]
    public async Task<IActionResult> UpdateFreelanceRecruiterVerify(
        [FromForm] UpdateFreelanceRecruiterVerifyRequest request)
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

        var detail = await context.FreelanceRecruiterDetails
            .SingleOrDefaultAsync(x => x.FreelanceRecruiterId == user.Id);

        if (detail == null)
        {
            return NotFound("Freelance recruiter detail not found.");
        }

        var idCardsFolder = Path.Combine(env.WebRootPath, "idCards");
        if (!Directory.Exists(idCardsFolder))
        {
            Directory.CreateDirectory(idCardsFolder);
        }

        if (request.FrontIdCard != null)
        {
            var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var frontIdCardExtension = Path.GetExtension(request.FrontIdCard.FileName).ToLowerInvariant();
            if (!allowedImageExtensions.Contains(frontIdCardExtension))
            {
                return BadRequest("Invalid front ID card file extension. Only image files are allowed.");
            }

            if (request.FrontIdCard.Length > 5 * 1024 * 1024)
            {
                return BadRequest("Front ID card file size exceeds the 5MB limit.");
            }

            var frontIdCard = await context.AttachedFiles
                .SingleOrDefaultAsync(f => f.TargetId == detail.Id && f.Type == TargetType.FrontIdCard);

            if (frontIdCard == null)
            {
                return NotFound("Front ID card not found.");
            }

            var frontIdCardFileName = $"{Guid.NewGuid()}{Path.GetExtension(request.FrontIdCard.FileName)}";
            var frontUrl = Path.Combine(idCardsFolder, frontIdCardFileName);

            await using (var stream = new FileStream(frontUrl, FileMode.Create))
            {
                await request.FrontIdCard.CopyToAsync(stream);
            }

            frontIdCard.Name = frontIdCardFileName;
            frontIdCard.Path = PathHelper.GetRelativePathFromAbsolute(frontUrl, env.WebRootPath);
            frontIdCard.UploadedById = user.Id;

            context.AttachedFiles.Update(frontIdCard);
        }

        if (request.BackIdCard != null)
        {
            var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var backIdCardExtension = Path.GetExtension(request.BackIdCard.FileName).ToLowerInvariant();
            if (!allowedImageExtensions.Contains(backIdCardExtension))
            {
                return BadRequest("Invalid back ID card file extension. Only image files are allowed.");
            }

            if (request.BackIdCard.Length > 5 * 1024 * 1024)
            {
                return BadRequest("Back ID card file size exceeds the 5MB limit.");
            }

            var backIdCard = await context.AttachedFiles
                .SingleOrDefaultAsync(f => f.TargetId == detail.Id && f.Type == TargetType.BackIdCard);

            if (backIdCard == null)
            {
                return NotFound("Back ID card not found.");
            }

            var backIdCardFileName = $"{Guid.NewGuid()}{Path.GetExtension(request.BackIdCard.FileName)}";
            var backUrl = Path.Combine(idCardsFolder, backIdCardFileName);

            await using (var stream = new FileStream(backUrl, FileMode.Create))
            {
                await request.BackIdCard.CopyToAsync(stream);
            }

            backIdCard.Name = backIdCardFileName;
            backIdCard.Path = PathHelper.GetRelativePathFromAbsolute(backUrl, env.WebRootPath);
            backIdCard.UploadedById = user.Id;

            context.AttachedFiles.Update(backIdCard);
        }

        detail.Status = VerifyStatus.Pending;
        try
        {
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error saving to database: {ex.Message}");
        }

        return Ok(new { Message = "Freelance recruiter registration updated successfully" });
    }
}