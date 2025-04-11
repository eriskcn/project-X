using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ProjectX.Data;

namespace ProjectX.Models;

public class AttachedFile : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] [StringLength(50)] public required string Name { get; set; }

    [Required] [StringLength(256)] public required string Path { get; set; }

    [Required]
    [Column(TypeName = "nvarchar(50)")]
    public TargetType Type { get; set; }

    [Required] public Guid TargetId { get; set; }

    [Required] public Guid UploadedById { get; set; }

    [JsonIgnore]
    [ForeignKey("UploadedById")]
    public User UploadedBy { get; set; } = null!;

    public DateTime Uploaded { get; set; } = DateTime.UtcNow;
}

public enum TargetType
{
    Application,
    BusinessRegistration,
    JobDescription,
    FrontIdCard, 
    BackIdCard,
    MessageAttachment,
    PostAttachment
}