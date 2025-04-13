using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Models;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/messages")]
[Authorize]
public class MessageController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet("history/{receiverId:guid}")]
    public async Task<ActionResult<IEnumerable<MessageResponse>>> GetMessageHistory(
        [FromRoute] Guid receiverId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized("Invalid or missing user ID in access token.");
        }

        if (page < 1 || pageSize < 1)
        {
            return BadRequest("Page and pageSize must be positive integers.");
        }

        var receiver = await context.Users.FindAsync(receiverId);
        if (receiver == null)
        {
            return NotFound("Receiver not found.");
        }

        var sender = await context.Users.FindAsync(userId);
        if (sender == null)
        {
            return NotFound("Sender not found.");
        }
        
        var messagesQuery = context.Messages
            .Where(m => (m.SenderId == userId && m.ReceiverId == receiverId) ||
                        (m.SenderId == receiverId && m.ReceiverId == userId))
            .OrderByDescending(m => m.Created)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var messages = await messagesQuery.ToListAsync();

        var userIds = messages
            .SelectMany(m => new[] { m.SenderId, m.ReceiverId })
            .Distinct()
            .ToList();

        var users = await context.Users
            .Where(u => userIds.Contains(u.Id))
            .Include(u => u.CompanyDetail)
            .ToListAsync();

        var userDictionary = users.ToDictionary(u => u.Id, u => new UserResponse
        {
            Id = u.Id,
            Name = u.CompanyDetail != null ? u.CompanyDetail.CompanyName : u.FullName,
            ProfilePicture = u.CompanyDetail != null ? u.CompanyDetail.Logo : u.ProfilePicture
        });

        var messageIds = messages.Select(m => m.Id).ToList();
        var attachedFiles = await context.AttachedFiles
            .Where(f => f.Type == TargetType.MessageAttachment && messageIds.Contains(f.TargetId))
            .ToListAsync();

        var attachedFileDict = attachedFiles.ToDictionary(f => f.TargetId, f => new FileResponse
        {
            Id = f.Id,
            Name = f.Name,
            Path = f.Path,
            Uploaded = f.Uploaded
        });

        var response = messages.Select(m => new MessageResponse
        {
            Id = m.Id,
            Content = m.Content ?? string.Empty,
            Sender = userDictionary.GetValueOrDefault(m.SenderId)!,
            Receiver = userDictionary.GetValueOrDefault(m.ReceiverId)!,
            IsEdited = m.IsEdited,
            Edited = m.Edited,
            IsRead = m.IsRead,
            Read = m.Read,
            AttachedFile = attachedFileDict.GetValueOrDefault(m.Id),
            Created = m.Created
        }).ToList();

        return Ok(response);
    }
}