using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectX.Models;

public class File
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public required string Id { get; set; }

    public required string Name { get; set; }
    public required string Path { get; set; }
    public string? FileHash { get; set; }

    // Relationship
    public required string UploadedById { get; set; }
    [ForeignKey("UploadedById")] public required User UploadedBy { get; set; }
    
    public CompanyDetail CompanyDetail { get; set; } = null!;

    // Tracking
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime Uploaded { get; set; } = DateTime.UtcNow;
}