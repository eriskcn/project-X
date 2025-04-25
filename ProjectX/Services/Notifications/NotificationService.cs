using Microsoft.AspNetCore.SignalR;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Models;
using ProjectX.Hubs;

namespace ProjectX.Services.Notifications;

public class NotificationService(IHubContext<NotificationHub> hubContext, ApplicationDbContext dbContext)
    : INotificationService
{
    public async Task SendNotificationAsync(NotificationRequest notificationRequest)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            Type = notificationRequest.Type,
            RecipientId = notificationRequest.RecipientId,
            TargetId = notificationRequest.TargetId
        };

        dbContext.Add(notification);
        await dbContext.SaveChangesAsync();

        var notificationResponse = new NotificationResponse
        {
            Id = notification.Id,
            Type = notification.Type,
            RecipientId = notification.RecipientId,
            TargetId = notification.TargetId,
            IsRead = notification.IsRead,
            Read = notification.Read,
            Created = notification.Created
        };
        await hubContext.Clients.User(notificationRequest.RecipientId.ToString())
            .SendAsync("ReceiveNotification", notificationResponse);
    }
}