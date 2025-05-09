using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Helpers;
using ProjectX.Models;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/users")]
[Authorize]
public class UserController(ApplicationDbContext context, UserManager<User> userManager, IWebHostEnvironment env)
    : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<ProfileInfoResponse>> GetMe()
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized(new { Message = "User Id not found in access token" });
        }

        var user = await context.Users.Include(u => u.CompanyDetail)
            .SingleOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        var userRoles = await userManager.GetRolesAsync(user);

        return Ok(new ProfileInfoResponse
        {
            Id = userId,
            FullName = user.CompanyDetail != null ? user.CompanyDetail.CompanyName : user.FullName,
            Email = user.Email ?? string.Empty,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            ProfilePicture = user.CompanyDetail != null ? user.CompanyDetail.Logo : user.ProfilePicture,
            GitHubProfile = user.GitHubProfile,
            LinkedInProfile = user.LinkedInProfile,
            Roles = userRoles,
            RecruiterVerified = user.RecruiterVerified,
            VerificationSubmitted = user.VerificationSubmitted,
            XTokenBalance = user.XTokenBalance,
            IsElite = user.Level == AccountLevel.Elite,
            IsExternalLogin = user.IsExternalLogin,
            Provider = user.Provider,
            Status = user.Status
        });
    }

    [HttpPatch]
    [Authorize(Policy = "EmailConfirmed")]
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

        var avatarsFolder = Path.Combine(env.WebRootPath, "avatars");
        if (!Directory.Exists(avatarsFolder))
        {
            Directory.CreateDirectory(avatarsFolder);
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

        if (request.ProfilePicture is { Length: > 0 })
        {
            var ext = Path.GetExtension(request.ProfilePicture.FileName).ToLower();
            if (!allowedExtensions.Contains(ext))
            {
                return BadRequest(new { Message = "Invalid file type" });
            }

            if (request.ProfilePicture.Length > 10 * 1024 * 1024)
            {
                return BadRequest(new { Message = "File size too large" });
            }

            var avatarFileName = $"{Guid.NewGuid()}{ext}";
            var profilePicturePath = Path.Combine(avatarsFolder, avatarFileName);

            await using var stream = new FileStream(profilePicturePath, FileMode.Create);
            await request.ProfilePicture.CopyToAsync(stream);

            user.ProfilePicture = PathHelper.GetRelativePathFromAbsolute(profilePicturePath, env.WebRootPath);
        }

        await context.SaveChangesAsync();

        return Ok(new { Message = "Profile updated successfully" });
    }


    [HttpGet]
    [Authorize(Policy = "EmailConfirmed")]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetUsers([FromQuery] string? search)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return Unauthorized(new { Message = "Invalid access token" });
        }

        var user = await context.Users.FindAsync(parsedUserId);
        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        var baseQuery = context.Users
            .Include(u => u.CompanyDetail)
            .Where(u => u.Id != parsedUserId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();

            baseQuery = baseQuery.Where(u =>
                (u.CompanyDetail != null && u.CompanyDetail.CompanyName.ToLower().Contains(search)) ||
                (u.CompanyDetail == null && u.FullName.ToLower().Contains(search)));
        }

        var usersList = await baseQuery
            .Select(u => new UserResponse
            {
                Id = u.Id,
                Name = u.CompanyDetail != null ? u.CompanyDetail.CompanyName : u.FullName,
                ProfilePicture = u.CompanyDetail != null ? u.CompanyDetail.Logo : u.ProfilePicture
            })
            .ToListAsync();

        return Ok(usersList);
    }
}