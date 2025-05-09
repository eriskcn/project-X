using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Models;
using ProjectX.Services;
using ProjectX.Services.Email;
using ProjectX.Services.GoogleAuth;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/auth")]
public class AuthController(
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    ApplicationDbContext context,
    IGoogleAuthService googleAuthService,
    ITokenService tokenService,
    IEmailService emailService)
    : ControllerBase
{
    [HttpPost("sign-up")]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                    { Message = "Invalid request data", Errors = ModelState.Values.SelectMany(v => v.Errors) });

            var existingUser = await userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return Conflict(new { Message = "Email is already in use" });
            }

            var roleExists = await roleManager.RoleExistsAsync(request.RoleName);
            if (!roleExists)
            {
                return BadRequest(new { Message = $"Role '{request.RoleName}' does not exist" });
            }

            var user = new User
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                EmailConfirmed = false
            };

            var createResult = await userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                return BadRequest(new
                {
                    Message = "User creation failed",
                    Errors = createResult.Errors.Select(e => e.Description)
                });
            }

            var addToRoleResult = await userManager.AddToRoleAsync(user, request.RoleName);
            if (!addToRoleResult.Succeeded)
            {
                await userManager.DeleteAsync(user);
                return BadRequest(new
                {
                    Message = $"Failed to add user to role '{request.RoleName}'",
                    Errors = addToRoleResult.Errors.Select(e => e.Description)
                });
            }

            await emailService.SendOtpViaEmailAsync(user.Email);

            var accessToken = await tokenService.GenerateAccessTokenAsync(user);
            var refreshToken = tokenService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await userManager.UpdateAsync(user);

            Response.Cookies.Append("AccessToken", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None
            });
            Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None
            });

            return Ok(new
            {
                Message = "User created successfully. Please check your email for verification.",
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Message = "An error occurred while processing your request",
                Error = ex.Message
            });
        }
    }

    [HttpPost("resend-otp")]
    [Authorize]
    public async Task<IActionResult> ResendOtp()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized(new { Message = "User not authenticated" });
        }

        var user = await context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == Guid.Parse(userIdString));
        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        if (user.EmailConfirmed)
        {
            return BadRequest(new { Message = "Email already confirmed" });
        }

        var email = user.Email;
        if (string.IsNullOrEmpty(email))
        {
            return BadRequest(new { Message = "Email not found" });
        }

        await emailService.SendOtpViaEmailAsync(email);
        return Ok(new { Message = "OTP resent successfully" });
    }

    [HttpPost("confirm-email")]
    [Authorize]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized(new { Message = "User not authenticated" });
        }

        var user = await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == Guid.Parse(userIdString));
        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        if (user.EmailConfirmed)
        {
            return BadRequest(new { Message = "Email already confirmed" });
        }

        if (user.OTP != request.Otp)
        {
            return BadRequest(new { Message = "Invalid OTP" });
        }

        user.EmailConfirmed = true;
        user.OTP = null;
        user.Modified = DateTime.UtcNow;
        context.Update(user);
        await context.SaveChangesAsync();
        return Ok(new { Message = "Email confirmed successfully" });
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
            SameSite = SameSiteMode.None
        });
        Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None
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

        var userToUpdate = await context.Users
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

        Response.Cookies.Append("AccessToken", newAccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None
        });
        Response.Cookies.Append("RefreshToken", newRefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None
        });

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

            var user = await context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == Guid.Parse(userIdString));
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            user.RefreshToken = null;
            user.RefreshTokenExpiry = DateTime.UtcNow;

            context.Update(user);
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


    [HttpPost("google-auth")]
    public async Task<IActionResult> GoogleAuth([FromBody] GoogleAuthRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (string.Equals(request.RoleName, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { Message = "Admin role cannot be assigned via this endpoint" });
        }

        var payload = await googleAuthService.VerifyGoogleTokenAsync(request.IdToken);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
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
                    EmailConfirmed = true,
                    Modified = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(result.Errors);
                }

                if (!await roleManager.RoleExistsAsync(request.RoleName))
                {
                    return BadRequest(new { Message = "Role not found" });
                }

                await userManager.AddToRoleAsync(user, request.RoleName);
            }

            var accessToken = await tokenService.GenerateAccessTokenAsync(user);
            var refreshToken = tokenService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            user.LoginAttempts++;
            await userManager.UpdateAsync(user);

            await transaction.CommitAsync();

            Response.Cookies.Append("AccessToken", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
            });

            Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
            });

            return Ok(new { Message = "Sign-in successful" });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { Message = "An error occurred during sign-in", Error = ex.Message });
        }
    }

    [HttpPatch("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await emailService.SendNewPasswordViaEmailAsync(request.Email);
        return Ok(new { Message = "Please check your email to receive a temporary password for your account." });
    }

    [HttpPatch("change-password")]
    [Authorize(Policy = "EmailConfirmed")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized(new { Message = "Invalid user ID." });

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        var result = await userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);

        if (result.Succeeded) return Ok(new { Message = "Change password successfully." });

        var errors = result.Errors.Select(e => e.Description);
        return BadRequest(new { Message = "Change password failed.", Errors = errors });
    }
}