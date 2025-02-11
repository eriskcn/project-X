using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectX.Models;

public class Message
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public required string Id { get; set; }

    [StringLength(1000)] public string? Content { get; set; }
    public bool IsDeleted { get; set; } = false;
    public bool IsRead { get; set; } = false;
    public bool IsEdited { get; set; } = false;

    // Relationship
    [Required] [StringLength(450)] public required string SenderId { get; set; }
    [ForeignKey("SenderId")] public User Sender { get; set; } = null!;

    [Required] [StringLength(450)] public required string ReceiverId { get; set; }
    [ForeignKey("ReceiverId")] public User Receiver { get; set; } = null!;

    [Required] [StringLength(450)] public required string ConversationId { get; set; }
    [ForeignKey("ConversationId")] public Conversation Conversation { get; set; } = null!;
    
    [StringLength(450)] public string? AttachedFileId { get; set; }
    [ForeignKey("AttachedFileId")] public File? AttachedFile { get; set; }

    // Tracking
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}