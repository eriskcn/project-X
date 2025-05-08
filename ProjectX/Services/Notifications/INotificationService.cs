using ProjectX.DTOs;
using ProjectX.Models;

namespace ProjectX.Services.Notifications;

public interface INotificationService
{
    Task SendNotificationAsync(NotificationType type, Guid recipientId, Guid targetId);
}