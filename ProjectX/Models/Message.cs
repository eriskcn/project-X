using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProjectX.Data;

namespace ProjectX.Models;

public class Message : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [StringLength(1000)] public string? Content { get; set; }

    public bool IsRead { get; set; }
    public DateTime? Read { get; set; }

    public bool IsEdited { get; set; }
    public DateTime? Edited { get; set; }

    // Relationship
    [Required] public Guid SenderId { get; set; }
    [ForeignKey("SenderId")] public User Sender { get; set; } = null!;

    [Required] public Guid ConversationId { get; set; }
    [ForeignKey("ConversationId")] public Conversation Conversation { get; set; } = null!;

    // Tracking
    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
}