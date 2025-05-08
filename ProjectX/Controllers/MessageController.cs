using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Helpers;
using ProjectX.Hubs;
using ProjectX.Models;


namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/messages")]
public class MessageController(
    ApplicationDbContext context,
    IHubContext<MessageHub> hubContext,
    IWebHostEnvironment env,
    ILogger<MessageController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<MessageResponse>> SendMessage([FromForm] MessageRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(senderId, out var senderGuid))
            return Unauthorized("Invalid sender ID.");

        if (senderGuid == request.ReceiverId)
        {
            return BadRequest(new { Message = "Sender and receiver cannot be the same." });
        }

        var sender = await context.Users
            .Include(u => u.CompanyDetail)
            .SingleOrDefaultAsync(u => u.Id == senderGuid);
        if (sender == null)
            return NotFound("Sender not found.");

        var receiver = await context.Users.FindAsync(request.ReceiverId);
        if (receiver == null)
            return NotFound("Receiver not found.");

        var conversation = await context.Conversations
            .Include(c => c.Participants)
            .SingleOrDefaultAsync(c =>
                !c.IsGroup &&
                c.Participants.Any(p => p.Id == senderGuid) &&
                c.Participants.Any(p => p.Id == request.ReceiverId));

        if (conversation == null)
        {
            var users = new List<User> { sender, receiver };
            conversation = new Conversation
            {
                IsGroup = false,
                Participants = users,
                Created = DateTime.UtcNow
            };
            context.Conversations.Add(conversation);
            await context.SaveChangesAsync();
        }

        var message = new Message
        {
            Content = request.Content,
            SenderId = senderGuid,
            ConversationId = conversation.Id,
            Created = DateTime.UtcNow
        };

        AttachedFile? attachedFile = null;
        if (request.AttachedFile != null)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
            var fileExtension = Path.GetExtension(request.AttachedFile.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest("Invalid file type.");

            if (request.AttachedFile.Length > 10 * 1024 * 1024)
                return BadRequest("File size exceeds 10MB.");

            var uploadsFolder = Path.Combine(env.WebRootPath, "messageAttachments");
            Directory.CreateDirectory(uploadsFolder);

            var cleanFileName = PathHelper.GetCleanFileName(request.AttachedFile.FileName);
            var displayName = Path.GetFileName(cleanFileName);

            var uniqueFileName = $"{Path.GetFileNameWithoutExtension(cleanFileName)}_{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.AttachedFile.CopyToAsync(stream);
            }

            attachedFile = new AttachedFile
            {
                Name = displayName,
                Path = PathHelper.GetRelativePathFromAbsolute(filePath, env.WebRootPath),
                Uploaded = DateTime.UtcNow,
                UploadedById = senderGuid,
                Type = FileType.MessageAttachment
            };
        }

        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            try
            {
                context.Messages.Add(message);
                await context.SaveChangesAsync();

                if (attachedFile != null)
                {
                    attachedFile.TargetId = message.Id;
                    context.AttachedFiles.Add(attachedFile);
                    await context.SaveChangesAsync();
                }

                conversation.LatestMessage = DateTime.UtcNow;
                conversation.LatestMessageId = message.Id;
                context.Conversations.Update(conversation);
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        var messageResponse = new MessageResponse
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            Content = message.Content,
            Created = message.Created,
            Sender = new UserResponse
            {
                Id = sender.Id,
                Name = sender.CompanyDetail?.CompanyName ?? sender.FullName,
                ProfilePicture = sender.CompanyDetail?.Logo ?? sender.ProfilePicture
            },
            AttachedFile = attachedFile != null
                ? new FileResponse
                {
                    Id = attachedFile.Id,
                    TargetId = attachedFile.TargetId,
                    Name = attachedFile.Name,
                    Path = attachedFile.Path,
                    Uploaded = attachedFile.Uploaded
                }
                : null
        };

        await hubContext.Clients.User(request.ReceiverId.ToString()).SendAsync("ReceiveMessage", messageResponse);

        return Ok(messageResponse);
    }

    [HttpPatch("{messageId:guid}/mark-as-read")]
    public async Task<ActionResult<MessageResponse>> MarkAsRead(Guid messageId,
        [FromQuery] bool markOlderAsRead = true)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized("Invalid user ID.");

        var message = await context.Messages
            .Include(m => m.Conversation)
            .ThenInclude(c => c.Participants)
            .Include(m => m.Sender)
            .ThenInclude(u => u.CompanyDetail)
            .SingleOrDefaultAsync(m => m.Id == messageId
                                       && m.Conversation.Participants.Any(p => p.Id == userGuid));

        if (message == null)
        {
            return NotFound("Message not found.");
        }

        if (message.SenderId == userGuid)
        {
            return Unauthorized(new { Message = "Sender cannot mark their own message as read." });
        }

        if (message.IsRead && !markOlderAsRead)
        {
            return Ok(new { Message = "Message was already read." });
        }

        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            try
            {
                var now = DateTime.UtcNow;

                if (markOlderAsRead)
                {
                    var unreadMessages = await context.Messages
                        .Where(m => m.ConversationId == message.ConversationId
                                    && m.Id <= message.Id
                                    && !m.IsRead
                                    && m.SenderId != userGuid)
                        .ToListAsync();

                    foreach (var msg in unreadMessages)
                    {
                        msg.IsRead = true;
                        msg.Read = now;
                    }
                }
                else
                {
                    message.IsRead = true;
                    message.Read = now;
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "Error marking messages as read");
                throw;
            }
        }

        var response = new MessageResponse
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            Content = message.Content,
            Created = message.Created,
            Sender = new UserResponse
            {
                Id = message.SenderId,
                Name = message.Sender.CompanyDetail?.CompanyName ?? message.Sender.FullName,
                ProfilePicture = message.Sender.CompanyDetail?.Logo
                                 ?? message.Sender.ProfilePicture
            },
            AttachedFile = await context.AttachedFiles
                .Where(f => f.Type == FileType.MessageAttachment
                            && f.TargetId == message.Id)
                .Select(f => new FileResponse
                {
                    Id = f.Id,
                    TargetId = f.TargetId,
                    Name = f.Name,
                    Path = f.Path,
                    Uploaded = f.Uploaded
                })
                .SingleOrDefaultAsync(),
            IsRead = true,
            Read = DateTime.UtcNow,
            Edited = message.Edited,
            IsEdited = message.IsEdited
        };

        await hubContext.Clients.User(message.SenderId.ToString())
            .SendAsync("MessageRead", response);

        return Ok(response);
    }

    [HttpPatch("{messageId:guid}")]
    public async Task<ActionResult<MessageResponse>> EditMessage([FromRoute] Guid messageId,
        [FromBody] UpdateMessageRequest request)
    {
        var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(senderId, out var senderGuid))
        {
            return Unauthorized(new { Message = "Invalid sender ID." });
        }

        var message = await context.Messages
            .Include(m => m.Conversation)
            .ThenInclude(c => c.Participants)
            .Include(m => m.Sender)
            .ThenInclude(u => u.CompanyDetail)
            .SingleOrDefaultAsync(m => m.Id == messageId
                                       && m.Conversation.Participants.Any(p => p.Id == senderGuid));

        if (message == null)
        {
            return NotFound(new { Message = "Message not found." });
        }

        if (message.SenderId != senderGuid)
        {
            return Unauthorized(new { Message = "You are not authorized to edit this message." });
        }

        message.Content = request.Content;
        message.IsEdited = true;
        message.Edited = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var receiver = message.Conversation.Participants
            .SingleOrDefault(p => p.Id != senderGuid);

        var response = new MessageResponse
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            Content = message.Content,
            Created = message.Created,
            Sender = new UserResponse
            {
                Id = message.Sender.Id,
                Name = message.Sender.CompanyDetail?.CompanyName ?? message.Sender.FullName,
                ProfilePicture = message.Sender.CompanyDetail?.Logo ?? message.Sender.ProfilePicture
            },
            AttachedFile = context.AttachedFiles
                .Where(f => f.Type == FileType.MessageAttachment && f.TargetId == message.Id)
                .Select(f => new FileResponse
                {
                    Id = f.Id,
                    TargetId = f.TargetId,
                    Name = f.Name,
                    Path = f.Path,
                    Uploaded = f.Uploaded
                })
                .SingleOrDefault(),
            IsRead = message.IsRead,
            Read = message.Read,
            Edited = message.Edited,
            IsEdited = message.IsEdited
        };

        if (receiver != null)
        {
            await hubContext.Clients.User(receiver.Id.ToString()).SendAsync("MessageEdited", response);
        }

        return Ok(response);
    }
}