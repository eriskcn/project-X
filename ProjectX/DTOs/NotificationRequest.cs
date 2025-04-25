using ProjectX.Models;

namespace ProjectX.DTOs;

public class NotificationRequest
{
    public NotificationType Type { get; set; }
    public Guid RecipientId { get; set; }
    public Guid TargetId { get; set; }
}