using ProjectX.Models;

namespace ProjectX.DTOs;

public class NotificationResponse
{
    public Guid Id { get; set; }
    public NotificationType Type { get; set; }
    public Guid RecipientId { get; set; }
    public Guid TargetId { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? Read { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
}