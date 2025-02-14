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
            FirstName = request.FirstName,
            MiddleName = request.MiddleName,
            LastName = request.LastName,
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


    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken()
    {
        if (!Request.Cookies.TryGetValue("RefreshToken", out var refreshToken))
        {
            return Unauthorized(new { Message = "Refresh token is missing" });
        }

        var user = await userManager.Users
            .AsNoTracking()
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.RefreshToken,
                u.RefreshTokenExpiry
            })
            .SingleOrDefaultAsync(u => u.RefreshToken == refreshToken);

        if (user == null || user.RefreshTokenExpiry <= DateTime.UtcNow)
        {
            return Unauthorized(new { Message = "Invalid or expired refresh token" });
        }

        // Create a new User object to update the refresh token
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
}