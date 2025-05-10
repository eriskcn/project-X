using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectX.Data;
using ProjectX.DTOs.Stats;
using ProjectX.Services.Stats;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/stats")]
public class StatsController(ApplicationDbContext context, IStatsService statsService) : ControllerBase
{
    [HttpGet("admin")]
    [Authorize(Roles = "Admin", Policy = "EmailConfirmed")]
    public async Task<ActionResult<AdminStats>> GetAdminStats()
    {
        var response = await statsService.GetAdminStats();
        return Ok(response);
    }

    [HttpGet("recruitment")]
    [Authorize(Roles = "Business, FreelanceRecruiter")]
    public async Task<ActionResult> GetRecruitmentStats()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized(new { Message = "Invalid user Id." });
        }

        var user = await context.Users.FindAsync(userGuid);
        if (user == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        var response = await statsService.GetRecruitmentStats(user);

        return Ok(response);
    }
}