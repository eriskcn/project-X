using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Helpers;
using ProjectX.Models;

namespace ProjectX.Hubs;

[Authorize]
public class MessageHub(
    ApplicationDbContext context,
    IWebHostEnvironment env,
    ConnectionMapping<Guid> connectionMapping) : Hub
{
    public async Task SendMessage(MessageRequest request)
    {
        var senderId = GetUserId();

        if (request.Content == null && request.AttachedFile == null)
        {
            throw new HubException("Message must contain either text content or an attached file.");
        }

        var receiver = await context.Users.FindAsync(request.ReceiverId);
        if (receiver == null)
        {
            throw new HubException("Receiver not found.");
        }

        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = request.ReceiverId,
            Content = request.Content,
            Created = DateTime.UtcNow,
            IsRead = false
        };

        context.Messages.Add(message);
        await context.SaveChangesAsync();

        if (request.AttachedFile != null)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".docx", ".pdf" };
            var fileExtension = Path.GetExtension(request.AttachedFile.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                throw new HubException("Invalid file extension. Only images and PDFs are allowed.");
            }

            if (request.AttachedFile.Length > 5 * 1024 * 1024)
            {
                throw new HubException("File size exceeds the 5MB limit.");
            }

            var messageAttachmentsFolder = Path.Combine(env.WebRootPath, "messageAttachments");
            if (!Directory.Exists(messageAttachmentsFolder))
            {
                Directory.CreateDirectory(messageAttachmentsFolder);
            }

            var uniqueFileName =
                $"{Guid.NewGuid():N}_{Path.GetFileNameWithoutExtension(request.AttachedFile.FileName)}{fileExtension}";
            var filePath = Path.Combine(messageAttachmentsFolder, uniqueFileName);

            try
            {
                await using var stream = new FileStream(filePath, FileMode.CreateNew);
                await request.AttachedFile.CopyToAsync(stream);
            }
            catch (Exception ex)
            {
                context.Messages.Remove(message);
                await context.SaveChangesAsync();

                Console.WriteLine($"File saving error: {ex.Message}");

                throw new HubException("An error occurred while saving the file.");
            }

            var file = new AttachedFile
            {
                Name = request.AttachedFile.FileName,
                Path = PathHelper.GetRelativePathFromAbsolute(filePath, env.WebRootPath),
                Type = TargetType.MessageAttachment,
                TargetId = message.Id,
                Uploaded = DateTime.UtcNow
            };

            context.AttachedFiles.Add(file);
            await context.SaveChangesAsync();
        }

        var connections = connectionMapping.GetConnections(request.ReceiverId);
        if (connections.Count > 0)
        {
            await Clients.User(request.ReceiverId.ToString())
                .SendAsync("ReceiveMessage", message.Id, request.Content, request.AttachedFile?.FileName);
        }
    }

    public async Task MarkAsRead(Guid messageId)
    {
        var userId = GetUserId();

        var message = await context.Messages.FindAsync(messageId);
        if (message == null || message.ReceiverId != userId)
        {
            throw new HubException("Message not found or you are not authorized to mark it as read.");
        }

        message.IsRead = true;
        message.Read = DateTime.UtcNow;

        await context.SaveChangesAsync();

        await Clients.User(message.SenderId.ToString()).SendAsync("MessageRead", messageId);
    }

    public async Task EditMessage(Guid messageId, string newContent)
    {
        var userId = GetUserId();

        var message = await context.Messages.FindAsync(messageId);
        if (message == null || message.SenderId != userId)
        {
            throw new HubException("Message not found or you are not authorized to edit it.");
        }

        if (string.IsNullOrWhiteSpace(newContent))
        {
            throw new HubException("Message content cannot be empty.");
        }

        message.Content = newContent;
        message.IsEdited = true;
        message.Edited = DateTime.UtcNow;

        await context.SaveChangesAsync();

        await Clients.User(message.ReceiverId.ToString()).SendAsync("MessageEdited", messageId, newContent);
    }

    public async Task SyncMessages()
    {
        var userId = GetUserId();
        var unreadMessages = await context.Messages
            .Where(m => m.ReceiverId == userId && !m.IsRead)
            .ToListAsync();

        foreach (var message in unreadMessages)
        {
            await Clients.Caller.SendAsync("ReceiveMessage", message.Id, message.Content, null);
        }
    }


    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        connectionMapping.Add(userId, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        connectionMapping.Remove(userId, Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    private Guid GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new HubException("Invalid or missing user ID.");
        }

        return userId;
    }
}