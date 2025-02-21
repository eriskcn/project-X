using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProjectX.Data;

namespace ProjectX.Models;

public class AttachedFile : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] [StringLength(50)] public required string Name { get; set; }

    [Required] [StringLength(256)] public required string Path { get; set; }

    // [StringLength(256)] public string? FileHash { get; set; }

    [Required]
    [Column(TypeName = "nvarchar(50)")]
    public TargetType Type { get; set; }

    [Required] public Guid TargetId { get; set; }

    // Relationship - UploadedBy
    [Required] public Guid UploadedById { get; set; }
    [ForeignKey("UploadedById")] public User UploadedBy { get; set; } = null!;

    // Optional Relationships
    // public Application? Application { get; set; }
    // public Message? Messages { get; set; }
    // public CompanyDetail? CompanyDetail { get; set; }

    // Tracking
    public DateTime Uploaded { get; set; } = DateTime.UtcNow;
}

public enum TargetType
{
    Application,
    CompanyDetail,
    Message,
    Post
}