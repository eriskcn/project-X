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

    [StringLength(256)] public string? FileHash { get; set; }

    // Relationship - UploadedBy
    [Required] public Guid UploadedById { get; set; }
    [ForeignKey("UploadedById")] public required User UploadedBy { get; set; }

    // Optional Relationships
    public Application? Application { get; set; }
    public Message? Messages { get; set; }
    public CompanyDetail? CompanyDetail { get; set; }

    // Tracking
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime Uploaded { get; set; } = DateTime.UtcNow;
}