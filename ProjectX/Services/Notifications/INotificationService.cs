using ProjectX.DTOs;

namespace ProjectX.Services.Notifications;

public interface INotificationService
{
    Task SendNotificationAsync(NotificationRequest notificationRequest);
}