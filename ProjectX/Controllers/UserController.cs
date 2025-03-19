using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectX.Data;
using ProjectX.DTOs;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/users")]
[Authorize]
public class UserController(ApplicationDbContext context, IWebHostEnvironment env) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<ProfileInfoResponse>> GetProfile()
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized(new { Message = "Unauthorized" });
        }


        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        return Ok(new ProfileInfoResponse
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            ProfilePicture = user.ProfilePicture,
            GitHubProfile = user.GitHubProfile,
            LinkedInProfile = user.LinkedInProfile,
            BusinessVerified = user.BusinessVerified,
            BusinessPoints = user.BusinessPoints,
            IsExternalLogin = user.IsExternalLogin,
            Provider = user.Provider,
            Status = user.Status
        });
    }

    [HttpPatch]
    public async Task<ActionResult<ProfileInfoResponse>> UpdateProfile([FromForm] ProfileInfoRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty);
        if (userId == Guid.Empty)
        {
            return BadRequest(new { Message = "Invalid user" });
        }

        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        user.FullName = request.FullName ?? user.FullName;
        user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
        user.GitHubProfile = request.GitHubProfile ?? user.GitHubProfile;
        user.LinkedInProfile = request.LinkedInProfile ?? user.LinkedInProfile;

        var imagesFolder = Path.Combine(env.WebRootPath, "images");
        if (!Directory.Exists(imagesFolder))
        {
            Directory.CreateDirectory(imagesFolder);
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        if (request.ProfilePicture != null)
        {
            var ext = Path.GetExtension(request.ProfilePicture.FileName).ToLower();
            if (!allowedExtensions.Contains(ext))
            {
                return BadRequest(new { Message = "Invalid file type" });
            }

            if (request.ProfilePicture.Length > 5 * 1024 * 1024) 
            {
                return BadRequest(new { Message = "File size too large" });
            }

            var profilePicturePath = Path.Combine(imagesFolder, request.ProfilePicture.FileName);
            await using var stream = new FileStream(profilePicturePath, FileMode.Create);
            await request.ProfilePicture.CopyToAsync(stream);

            user.ProfilePicture = $"/images/{request.ProfilePicture.FileName}";
        }

        await context.SaveChangesAsync();

        return Ok(new ProfileInfoResponse
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            ProfilePicture = user.ProfilePicture,
            GitHubProfile = user.GitHubProfile,
            LinkedInProfile = user.LinkedInProfile,
            BusinessVerified = user.BusinessVerified,
            BusinessPoints = user.BusinessPoints,
            IsExternalLogin = user.IsExternalLogin,
            Provider = user.Provider,
            Status = user.Status
        });
    }
}