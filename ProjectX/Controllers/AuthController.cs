using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.DTOs;
using ProjectX.Models;
using ProjectX.Services;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/auth")]
public class AuthController(
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    GoogleAuthService googleAuthService,
    ITokenService tokenService)
    : ControllerBase
{
    [HttpPost("sign-up")]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (await userManager.FindByEmailAsync(request.Email) != null)
        {
            return BadRequest(new { Message = "Email is already in use" });
        }

        if (!await roleManager.RoleExistsAsync(request.RoleName))
        {
            var role = new Role { Name = request.RoleName };
            var roleResult = await roleManager.CreateAsync(role);
            if (!roleResult.Succeeded)
            {
                return BadRequest(new { Message = "Failed to create role" });
            }
        }

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            GitHubProfile = request.GitHubProfile,
            LinkedInProfile = request.LinkedInProfile,
            Modified = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        result = await userManager.AddToRoleAsync(user, request.RoleName);
        if (result.Succeeded) return Ok(new { Message = "User created successfully" });
        await userManager.DeleteAsync(user);
        return BadRequest(new { Message = "Failed to assign role" });
    }

    [HttpPost("sign-in")]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new { Message = "Invalid email or password" });
        }

        var accessToken = await tokenService.GenerateAccessTokenAsync(user);
        var refreshToken = tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        user.LoginAttempts++;
        await userManager.UpdateAsync(user);

        Response.Cookies.Append("AccessToken", accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddMinutes(30)
        });

        Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(7)
        });

        return Ok(new { Message = "Sign-in successful" });
    }

    [HttpGet("check-business-verified")]
    [Authorize(Roles = "Business")]
    public IActionResult CheckBusinessVerified()
    {
        var businessVerifiedClaim = User.FindFirst("BusinessVerified")?.Value;
        var isBusinessVerified = businessVerifiedClaim != null && bool.Parse(businessVerifiedClaim);

        return Ok(new
        {
            IsAuthenticated = true,
            IsBusinessVerified = isBusinessVerified
        });
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken()
    {
        if (!Request.Cookies.TryGetValue("RefreshToken", out var refreshToken))
        {
            return Unauthorized(new { Message = "Refresh token is missing" });
        }

        var user = await userManager.Users
            .Where(u => u.RefreshToken == refreshToken && u.RefreshTokenExpiry > DateTime.UtcNow)
            .SingleOrDefaultAsync();

        if (user == null || user.RefreshTokenExpiry <= DateTime.UtcNow)
        {
            return Unauthorized(new { Message = "Invalid or expired refresh token" });
        }

        var userToUpdate = await userManager.FindByIdAsync(user.Id.ToString());
        if (userToUpdate == null)
        {
            return Unauthorized(new { Message = "User not found" });
        }

        var newAccessToken = await tokenService.GenerateAccessTokenAsync(userToUpdate);
        var newRefreshToken = tokenService.GenerateRefreshToken();

        userToUpdate.RefreshToken = newRefreshToken;
        userToUpdate.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        userToUpdate.Modified = DateTime.UtcNow;

        var userUpdateResult = await userManager.UpdateAsync(userToUpdate);
        if (!userUpdateResult.Succeeded)
        {
            return StatusCode(500, new { Message = "Failed to update refresh token" });
        }

        Response.Cookies.Append("AccessToken", newAccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddMinutes(30)
        });

        Response.Cookies.Append("RefreshToken", newRefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(7)
        });

        return Ok(new { Message = "Token refreshed successfully" });
    }

    [Authorize]
    [HttpPost("log-out")]
    public async Task<IActionResult> LogOut()
    {
        Response.Cookies.Delete("AccessToken");
        Response.Cookies.Delete("RefreshToken");

        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized(new { Message = "User not found" });
        }

        user.RefreshToken = null;
        user.RefreshTokenExpiry = DateTime.UtcNow;

        var result = await userManager.UpdateAsync(user);
        return !result.Succeeded
            ? StatusCode(500, new { Message = "Failed to update user" })
            : Ok(new { Message = "Sign-out successful" });
    }
    
    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        var payload = await googleAuthService.VerifyGoogleTokenAsync(request.IdToken);

        var user = await userManager.FindByEmailAsync(payload.Email);
        if (user == null)
        {
            user = new User
            {
                UserName = payload.Email,
                Email = payload.Email,
                FullName = payload.Name,
                ProfilePicture = payload.Picture,
                Provider = "Google",
                OAuthId = payload.Subject,
                Modified = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }
        }

        var accessToken = await tokenService.GenerateAccessTokenAsync(user);
        var refreshToken = tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        user.LoginAttempts++;
        await userManager.UpdateAsync(user);

        Response.Cookies.Append("AccessToken", accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddMinutes(30)
        });

        Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(7)
        });

        return Ok(new { Message = "Sign-in successful" });
    }
}