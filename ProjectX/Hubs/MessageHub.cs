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
    ConnectionMapping<Guid> connectionMapping,
    ILogger<ConnectionMapping<Guid>> logger)
    : Hub
{
    private static readonly string[] AllowedFileExtensions = [".jpg", ".jpeg", ".png", ".docx", ".pdf"];
    private const long MaxFileSize = 5 * 1024 * 1024;
    private const string AttachmentsFolder = "messageAttachments";

    public async Task SendMessage(MessageRequest request)
    {
        var senderId = GetUserId();
        ValidateMessageRequest(request);

        var conversation = await GetOrCreateConversation(senderId, request.ReceiverId);
        var message = new Message
        {
            SenderId = senderId,
            ConversationId = conversation.Id,
            Content = request.Content,
            Created = DateTime.UtcNow,
            IsRead = false
        };
        context.Messages.Add(message);

        AttachedFile? attachedFile = null;
        if (request.AttachedFile != null)
        {
            attachedFile = await SaveAttachedFile(message.Id, request.AttachedFile);
            context.AttachedFiles.Add(attachedFile);
        }

        await context.SaveChangesAsync();

        conversation.LatestMessage = message.Created;
        conversation.LatestMessageId = message.Id;
        context.Conversations.Update(conversation);

        await context.SaveChangesAsync();

        await NotifyRecipient(
            request.ReceiverId,
            message.Id,
            message.Content ?? string.Empty,
            attachedFile?.Name);
    }

    public async Task MarkAsRead(Guid messageId)
    {
        var userId = GetUserId();

        var message = await context.Messages.FindAsync(messageId);
        if (message == null)
        {
            throw new HubException("Message not found.");
        }

        var conversation = await context.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == message.ConversationId);

        if (conversation == null || conversation.Participants.All(p => p.Id != userId))
        {
            throw new HubException("You are not authorized to mark this message as read.");
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

        await Clients.Group(message.ConversationId.ToString())
            .SendAsync("MessageEdited", messageId, newContent);
    }

    public async Task SyncMessages()
    {
        var userId = GetUserId();
        var unreadMessages = await context.Messages
            .Include(m => m.Conversation)
            .ThenInclude(c => c.Participants)
            .Where(m => !m.IsRead && m.Conversation.Participants.Any(p => p.Id == userId))
            .ToListAsync();

        var messageResponses = unreadMessages.Select(m => new
        {
            m.Id,
            m.Content,
            m.Created
        });

        await Clients.Caller.SendAsync("ReceiveMessagesBatch", messageResponses);
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        connectionMapping.Add(userId, Context.ConnectionId);
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());
            if (env.IsDevelopment())
            {
                logger.LogDebug("User {UserId} connected with ConnectionId {ConnectionId}", userId,
                    Context.ConnectionId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add ConnectionId {ConnectionId} to group {UserId}", Context.ConnectionId,
                userId);
            throw;
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        connectionMapping.Remove(userId, Context.ConnectionId);
        if (env.IsDevelopment())
        {
            logger.LogDebug("User {UserId} disconnected with ConnectionId {ConnectionId}", userId,
                Context.ConnectionId);
        }

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

    private static void ValidateMessageRequest(MessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content) && request.AttachedFile == null)
        {
            throw new HubException("Message must contain either text content or an attached file.");
        }
    }

    private async Task<Conversation> GetOrCreateConversation(Guid senderId, Guid receiverId)
    {
        var conversation = await context.Conversations
            .Include(c => c.Participants)
            .SingleOrDefaultAsync(c =>
                !c.IsGroup &&
                c.Participants.Count == 2 &&
                c.Participants.Any(p => p.Id == senderId) &&
                c.Participants.Any(p => p.Id == receiverId));

        if (conversation != null)
        {
            return conversation;
        }

        var sender = await context.Users.FindAsync(senderId);
        var receiver = await context.Users.FindAsync(receiverId);

        if (sender == null || receiver == null)
        {
            throw new HubException("Invalid sender or receiver.");
        }

        conversation = new Conversation
        {
            Participants = new List<User> { sender, receiver }
        };
        context.Conversations.Add(conversation);

        return conversation;
    }

    private async Task<AttachedFile> SaveAttachedFile(Guid messageId, IFormFile file)
    {
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedFileExtensions.Contains(fileExtension))
        {
            throw new HubException("Invalid file extension. Only images and PDFs are allowed.");
        }

        if (file.Length > MaxFileSize)
        {
            throw new HubException("File size exceeds the 5MB limit.");
        }

        if (!IsValidFileContent(file, fileExtension))
        {
            throw new HubException("Invalid file content.");
        }

        var attachmentsFolder = Path.Combine(env.WebRootPath, AttachmentsFolder);
        Directory.CreateDirectory(attachmentsFolder);

        var safeFileName = Path.GetFileName(file.FileName); // Sanitize file name
        var uniqueFileName = $"{Guid.NewGuid():N}_{Path.GetFileNameWithoutExtension(safeFileName)}{fileExtension}";
        var filePath = Path.Combine(attachmentsFolder, uniqueFileName);

        try
        {
            await using var stream = new FileStream(filePath, FileMode.CreateNew);
            await file.CopyToAsync(stream);
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "Failed to save file {FileName} for message {MessageId}", file.FileName, messageId);
            throw new HubException("An error occurred while saving the file.");
        }

        return new AttachedFile
        {
            Name = safeFileName,
            Path = PathHelper.GetRelativePathFromAbsolute(filePath, env.WebRootPath),
            Type = TargetType.MessageAttachment,
            TargetId = messageId,
            Uploaded = DateTime.UtcNow
        };
    }

    private async Task NotifyRecipient(Guid recipientId, Guid messageId, string content, string? fileName)
    {
        if (connectionMapping.HasConnection(recipientId, Context.ConnectionId))
        {
            await Clients.User(recipientId.ToString())
                .SendAsync("ReceiveMessage", messageId, content, fileName);
            if (env.IsDevelopment())
            {
                logger.LogDebug("Notified recipient {RecipientId} for message {MessageId}", recipientId, messageId);
            }
        }
        else
        {
            if (env.IsDevelopment())
            {
                logger.LogDebug("Recipient {RecipientId} has no active connections for message {MessageId}",
                    recipientId, messageId);
            }
        }
    }

    private bool IsValidFileContent(IFormFile file, string extension)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var buffer = new byte[4];
            var bytesRead = stream.Read(buffer, 0, buffer.Length);

            return extension switch
            {
                ".jpg" or ".jpeg" => bytesRead >= 2 && buffer[0] == 0xFF && buffer[1] == 0xD8,
                ".png" => bytesRead >= 4 && buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E &&
                          buffer[3] == 0x47,
                ".pdf" => bytesRead >= 4 && buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 &&
                          buffer[3] == 0x46,
                _ => true
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to validate file content for {FileName}", file.FileName);
            return false;
        }
    }
}