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

        var user = await context.Users.FindAsync(Guid.Parse(userId));
        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        var baseQuery = context.Conversations
            .Include(c => c.Participants)
            .ThenInclude(u => u.CompanyDetail)
            .Where(c => c.Participants.Any(p => p.Id.ToString() == userId));

        if (!string.IsNullOrEmpty(search))
        {
            baseQuery = baseQuery.Where(c => c.Participants.Any(p =>
                p.CompanyDetail != null
                    ? p.CompanyDetail.CompanyName.Contains(search)
                    : p.FullName.Contains(search)));
        }

        var conversationEntities = await baseQuery.ToListAsync();

        var responses = new List<ConversationResponse>();

        foreach (var c in conversationEntities)
        {
            var participant = c.Participants.SingleOrDefault(p => p.Id.ToString() != userId);
            if (participant == null)
            {
                throw new InvalidOperationException(
                    $"Conversation {c.Id} must contain at least one other participant.");
            }

            var response = new ConversationResponse
            {
                Id = c.Id,
                IsGroup = c.IsGroup,
                GroupName = c.GroupName ?? string.Empty,
                GroupPicture = c.GroupPicture ?? string.Empty,
                IsStored = c.IsStored,
                Participant = new UserResponse
                {
                    Id = participant.Id,
                    Name = participant.CompanyDetail?.CompanyName ?? participant.FullName,
                    ProfilePicture = participant.CompanyDetail?.Logo ?? participant.ProfilePicture
                },
                LatestMessage = c.LatestMessage,
                LatestMessageDetails = await context.Messages.Where(m => m.Id == c.LatestMessageId)
                    .Include(m => m.Sender)
                    .ThenInclude(u => u.CompanyDetail)
                    .Select(m => new MessageResponse
                    {
                        Id = m.Id,
                        ConversationId = m.Id,
                        Content = m.Content ?? string.Empty,
                        Created = m.Created,
                        Sender = new UserResponse
                        {
                            Id = m.Sender.Id,
                            Name = m.Sender.CompanyDetail != null
                                ? m.Sender.CompanyDetail.CompanyName
                                : m.Sender.FullName,
                            ProfilePicture = m.Sender.CompanyDetail != null
                                ? m.Sender.CompanyDetail.Logo
                                : m.Sender.ProfilePicture,
                        }
                    })
                    .SingleOrDefaultAsync(),
            };

            responses.Add(response);
        }

        var orderedResponses = responses
            .OrderByDescending(r => r.LatestMessageDetails?.Created);

        var pagedResponses = pageSize == 0
            ? orderedResponses.ToList()
            : orderedResponses.Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

        var totalCount = responses.Count;

        return Ok(new
        {
            Items = pagedResponses,
            PageNumber = page,
            PageSize = pageSize,
            TotalItems = totalCount,
            TotalPages = pageSize != 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 1
        });
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

        var participant = conversation.Participants
            .Where(p => p.Id.ToString() != userId)
            .Select(p => new UserResponse
            {
                Id = p.Id,
                Name = p.CompanyDetail != null
                    ? p.CompanyDetail.CompanyName
                    : p.FullName,
                ProfilePicture = p.CompanyDetail != null
                    ? p.CompanyDetail.Logo
                    : p.ProfilePicture
            })
            .SingleOrDefault();

        if (participant == null)
        {
            throw new InvalidOperationException(
                "Conversation must contain at least one participant other than the current user.");
        }


        var response = new ConversationResponse
        {
            Id = conversation.Id,
            IsGroup = conversation.IsGroup,
            GroupName = conversation.GroupName ?? string.Empty,
            GroupPicture = conversation.GroupPicture ?? string.Empty,
            IsStored = conversation.IsStored,
            Participant = participant,
            LatestMessage = conversation.LatestMessage,
            Messages = conversation.Messages
                .OrderByDescending(m => m.Created)
                .Select(m => new MessageResponse
                {
                    Id = m.Id,
                    ConversationId = m.ConversationId,
                    Content = m.Content ?? string.Empty,
                    Created = m.Created,
                    Sender = new UserResponse
                    {
                        Id = m.Sender.Id,
                        Name = m.Sender.CompanyDetail != null
                            ? m.Sender.CompanyDetail.CompanyName
                            : m.Sender.FullName,
                        ProfilePicture = m.Sender.CompanyDetail != null
                            ? m.Sender.CompanyDetail.Logo
                            : m.Sender.ProfilePicture
                    }
                })
                .ToList(),
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