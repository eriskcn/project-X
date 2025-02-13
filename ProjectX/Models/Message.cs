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
    public bool IsEdited { get; set; }

    // Relationship
    [Required] public Guid SenderId { get; set; }
    [ForeignKey("SenderId")] public User Sender { get; set; } = null!;

    [Required] public Guid ConversationId { get; set; }
    [ForeignKey("ConversationId")] public Conversation Conversation { get; set; } = null!;

    public Guid AttachedFileId { get; set; }
    [ForeignKey("AttachedFileId")] public AttachedFile? AttachedFile { get; set; }

    // Tracking
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}