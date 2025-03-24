using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Models;
using ProjectX.Services;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/auth")]
public class AuthController(
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    ApplicationDbContext context,
    IGoogleAuthService googleAuthService,
    ITokenService tokenService)
    : ControllerBase
{
    [HttpPost("sign-up")]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (await userManager.Users.AnyAsync(u => u.Email == request.Email))
        {
            return BadRequest(new { Message = "Email is already in use" });
        }

        if (!await roleManager.RoleExistsAsync(request.RoleName))
        {
            await roleManager.CreateAsync(new Role { Name = request.RoleName });
        }

        var user = new User { UserName = request.Email, Email = request.Email, FullName = request.FullName };
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded) return BadRequest(result.Errors);

        await userManager.AddToRoleAsync(user, request.RoleName);
        return Ok(new { Message = "User created successfully" });
    }

    [HttpPost("sign-in")]
    [AllowAnonymous]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new { Message = "Invalid email or password" });
        }

        var accessTokenTask = tokenService.GenerateAccessTokenAsync(user);
        var refreshToken = tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        user.LoginAttempts++;
        await userManager.UpdateAsync(user);

        Response.Cookies.Append("AccessToken", await accessTokenTask,
            new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None });
        Response.Cookies.Append("RefreshToken", refreshToken,
            new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None });

        return Ok(new { Message = "Sign-in successful" });
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken()
    {
        if (!Request.Cookies.TryGetValue("RefreshToken", out var refreshToken))
        {
            return Unauthorized(new { Message = "Refresh token is missing" });
        }

        var userToUpdate = await userManager.Users
            .Where(u => u.RefreshToken == refreshToken && u.RefreshTokenExpiry > DateTime.UtcNow)
            .SingleOrDefaultAsync();

        if (userToUpdate == null)
            return Unauthorized(new { Message = "Invalid or expired refresh token. Please log in again." });

        var newAccessToken = await tokenService.GenerateAccessTokenAsync(userToUpdate);
        var newRefreshToken = tokenService.GenerateRefreshToken();

        await context.Users
            .Where(u => u.Id == userToUpdate.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.RefreshToken, newRefreshToken)
                .SetProperty(u => u.RefreshTokenExpiry, DateTime.UtcNow.AddDays(7)));

        Response.Cookies.Append("AccessToken", newAccessToken,
            new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None });
        Response.Cookies.Append("RefreshToken", newRefreshToken,
            new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None });

        return Ok(new { Message = "Token refreshed successfully" });
    }

    [Authorize]
    [HttpPost("sign-out")]
    public async Task<IActionResult> LogOut()
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized(new { Message = "User not authenticated" });
            }

            var user = await context.Users.FindAsync(Guid.Parse(userIdString));
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            user.RefreshToken = null;
            user.RefreshTokenExpiry = DateTime.UtcNow;

            await context.SaveChangesAsync();

            Response.Cookies.Append("AccessToken", "", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(-1)
            });

            Response.Cookies.Append("RefreshToken", "", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(-1)
            });

            return Ok(new { Message = "Sign-out successful" });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { Message = "An error occurred during sign-out", Error = ex.Message });
        }
    }


    [HttpPost("google-sign-in")]
    public async Task<IActionResult> GoogleSignIn([FromBody] GoogleSignInRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

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
            // Expires = DateTime.UtcNow.AddMinutes(2)
        });

        Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            // Expires = DateTime.UtcNow.AddDays(7)
        });

        return Ok(new { Message = "Sign-in successful" });
    }
}