using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Models;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/campaigns")]
[Authorize(Roles = "Business, FreelanceRecruiter", Policy = "BusinessVerifiedOnly")]
public class CampaignController(ApplicationDbContext context) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CampaignResponse>> CreateCampaign([FromBody] CampaignRequest request)
    {
        var recruiterId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (recruiterId == null)
        {
            return Unauthorized(new { Message = "User ID not found in access token." });
        }

        var campaign = new Campaign
        {
            Name = request.Name,
            Description = request.Description,
            Open = request.Open,
            Close = request.Close,
            IsHighlight = request.IsHighlight,
            IsUrgent = request.IsUrgent,
            Status = request.Status,
            CountJobs = 0,
            RecruiterId = Guid.Parse(recruiterId)
        };

        context.Campaigns.Add(campaign);
        await context.SaveChangesAsync();

        var response = new CampaignResponse
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            Open = campaign.Open,
            Close = campaign.Close,
            IsHighlight = campaign.IsHighlight,
            IsUrgent = campaign.IsUrgent,
            CountJobs = campaign.CountJobs,
            Status = campaign.Status
        };

        return CreatedAtAction("", new { id = campaign.Id }, response);
    }


    // [HttpGet("{id:guid}")]
    // public async Task<IActionResult> GetCampaign(Guid id)
    // {
    //     var campaign = await context.Campaigns
    //         .Include(c => c.Jobs)
    //         .FirstOrDefaultAsync(c => c.Id == id);
    //
    //     if (campaign == null)
    //     {
    //         return NotFound(new { Message = "Campaign not found." });
    //     }
    //
    //     
    //
    // }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CampaignResponse>>> GetOwnCampaigns(
        [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new { Message = "Page number and page size must be greater than zero." });
        }

        var recruiterId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (recruiterId == null)
        {
            return Unauthorized(new { Message = "User ID not found in access token." });
        }

        var query = context.Campaigns
            .Where(c => c.RecruiterId == Guid.Parse(recruiterId))
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(c => c.Name.Contains(search) || c.Description.Contains(search));
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var campaigns = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CampaignResponse
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Open = c.Open,
                Close = c.Close,
                IsHighlight = c.IsHighlight,
                IsUrgent = c.IsUrgent,
                CountJobs = c.CountJobs,
                Status = c.Status
            })
            .ToListAsync();

        var response = new
        {
            TotalItems = totalItems,
            TotalPages = totalPages,
            PageNumber = page,
            PageSize = pageSize,
            Items = campaigns
        };

        return Ok(response);
    }
}