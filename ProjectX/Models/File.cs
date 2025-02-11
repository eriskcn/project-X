using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectX.Models;

public class File
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public required string Id { get; set; }

    [Required] [StringLength(50)] public required string Name { get; set; }
    [Required] [StringLength(256)] public required string Path { get; set; }
    [StringLength(256)] public string? FileHash { get; set; }

    // Relationship
    [Required]
    [StringLength(450)]
    public required string UploadedById { get; set; }
    [ForeignKey("UploadedById")] public required User UploadedBy { get; set; }

    public Application Application { get; set; } = null!;
    public Message Messages { get; set; } = null!;

    public CompanyDetail CompanyDetail { get; set; } = null!;

    // Tracking
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime Uploaded { get; set; } = DateTime.UtcNow;
}