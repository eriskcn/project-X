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
    IWebHostEnvironment env) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> SendMessage([FromForm] MessageRequest request)
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

            // Lưu file
            var uploadsFolder = Path.Combine(env.WebRootPath, "messageAttachments");
            Directory.CreateDirectory(uploadsFolder);
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.AttachedFile.CopyToAsync(stream);
            }

            attachedFile = new AttachedFile
            {
                Name = fileName,
                Path = PathHelper.GetRelativePathFromAbsolute(filePath, env.WebRootPath),
                Uploaded = DateTime.UtcNow,
                UploadedById = senderGuid,
                Type = TargetType.MessageAttachment
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

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Tạo response
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
}