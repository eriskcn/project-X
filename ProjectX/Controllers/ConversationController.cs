using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/conversations")]
[Authorize]
public class ConversationController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ConversationResponse>>> GetOwnConversations(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "Invalid access token" });
        }

        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        var baseQuery = context.Conversations
            .Include(c => c.Participants)
            .Where(c => c.Participants.Any(p => p.Id.ToString() == userId));

        if (!string.IsNullOrEmpty(search))
        {
            baseQuery = baseQuery.Where(c => c.Participants.Any(p =>
                p.CompanyDetail != null
                    ? p.CompanyDetail.CompanyName.Contains(search)
                    : p.FullName.Contains(search)));
        }

        var conversationsList = await baseQuery
            .Select(c => new
            {
                c.Id,
                c.IsGroup,
                c.GroupName,
                c.GroupPicture,
                c.IsStored,
                c.LatestMessage,
                c.LatestMessageId
            })
            .ToListAsync();

        var conversationIds = conversationsList.Select(c => c.Id).ToList();

        var latestMessages = await context.Messages
            .Where(m => conversationIds.Contains(m.ConversationId) &&
                        conversationsList.Any(c => c.LatestMessageId == m.Id))
            .Include(m => m.Sender)
            .ThenInclude(u => u.CompanyDetail)
            .ToDictionaryAsync(m => m.ConversationId);

        var orderedResponses = conversationsList
            .Select(c =>
            {
                latestMessages.TryGetValue(c.Id, out var latestMessage);
                return new ConversationResponse
                {
                    Id = c.Id,
                    IsGroup = c.IsGroup,
                    GroupName = c.GroupName ?? string.Empty,
                    GroupPicture = c.GroupPicture ?? string.Empty,
                    IsStored = c.IsStored,
                    LatestMessageDetails = latestMessage == null
                        ? null
                        : new MessageResponse
                        {
                            Id = latestMessage.Id,
                            Content = latestMessage.Content ?? string.Empty,
                            Created = latestMessage.Created,
                            Sender = new UserResponse
                            {
                                Id = latestMessage.Sender.Id,
                                Name = latestMessage.Sender.CompanyDetail?.CompanyName ??
                                       latestMessage.Sender.FullName,
                                ProfilePicture = latestMessage.Sender.CompanyDetail?.Logo ??
                                                 latestMessage.Sender.ProfilePicture
                            }
                        }
                };
            })
            .OrderByDescending(r => r.LatestMessageDetails?.Created);

        var responses = pageSize == 0
            ? orderedResponses.ToList()
            : orderedResponses.Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

        return Ok(responses);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ConversationResponse>> GetConversation(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "Invalid access token" });
        }

        var conversation = await context.Conversations
            .Include(c => c.Participants)
            .ThenInclude(u => u.CompanyDetail)
            .Include(c => c.Messages)
            .ThenInclude(m => m.Sender)
            .ThenInclude(u => u.CompanyDetail)
            .SingleOrDefaultAsync(c => c.Id == id && c.Participants.Any(p => p.Id.ToString() == userId));

        if (conversation == null)
        {
            return NotFound(new { Message = "Conversation not found" });
        }

        var response = new ConversationResponse
        {
            Id = conversation.Id,
            IsGroup = conversation.IsGroup,
            GroupName = conversation.GroupName ?? string.Empty,
            GroupPicture = conversation.GroupPicture ?? string.Empty,
            IsStored = conversation.IsStored,
            LatestMessage = conversation.LatestMessage,
            LatestMessageDetails = conversation.Messages
                .OrderByDescending(m => m.Created)
                .Select(m => new MessageResponse
                {
                    Id = m.Id,
                    Content = m.Content ?? string.Empty,
                    Created = m.Created,
                    Sender = new UserResponse
                    {
                        Id = m.Sender.Id,
                        Name = m.Sender.CompanyDetail?.CompanyName ?? m.Sender.FullName,
                        ProfilePicture = m.Sender.CompanyDetail?.Logo ?? m.Sender.ProfilePicture
                    }
                })
                .FirstOrDefault()
        };

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteConversation(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "Invalid access token" });
        }

        var conversation = await context.Conversations
            .Include(c => c.Participants)
            .SingleOrDefaultAsync(c => c.Id == id && c.Participants.Any(p => p.Id.ToString() == userId));

        if (conversation == null)
        {
            return NotFound(new { Message = "Conversation not found" });
        }

        context.Conversations.Remove(conversation);
        await context.SaveChangesAsync();

        return NoContent();
    }
}