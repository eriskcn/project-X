using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ProjectX.Data;

namespace ProjectX.Models;

public class Notification : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Content { get; set; }
    public NotificationType Type { get; set; }

    public Guid? ActorId { get; set; }
    [JsonIgnore] [ForeignKey("ActorId")] public User? Actor { get; set; } = null!;

    public Guid RecipientId { get; set; }

    [JsonIgnore]
    [ForeignKey("RecipientId")]
    public User Recipient { get; set; } = null!;

    public Guid TargetId { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? Read { get; set; }

    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}

public enum NotificationType
{
    SuccessfulApply, 
    NewReactToPost, 
    NewApplication,
    UpdateApplicationProcess, 
    NewComment, 
}