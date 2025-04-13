using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
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

    [Required] public Guid SenderId { get; set; }
    [JsonIgnore] [ForeignKey("SenderId")] public User Sender { get; set; } = null!;

    [Required] public Guid ReceiverId { get; set; }

    [JsonIgnore]
    [ForeignKey("ReceiverId")]
    public User Receiver { get; set; } = null!;

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
}