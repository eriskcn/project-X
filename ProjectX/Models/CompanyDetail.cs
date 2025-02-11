using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectX.Models;

public class CompanyDetail
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public required string Id { get; set; }

    [StringLength(450)] public required string Name { get; set; }

    [Required] [StringLength(10)] public string? ShortName { get; set; }
    [Required] [StringLength(256)] public required string HeadQuarterAddress { get; set; }
    [Required] [StringLength(256)] public required string Logo { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(50)]
    public required string ContactEmail { get; set; }

    [Range(1900, 2100)] public required int FoundedYear { get; set; }
    [Required] [StringLength(2500)] public required string Introduction { get; set; }

    // For relationship
    [Required] [StringLength(450)] public required string CompanyId { get; set; }
    [ForeignKey("CompanyId")] public User Company { get; set; } = null!;

    [Required] [StringLength(450)] public required string RegistrationFileId { get; set; }
    [ForeignKey("RegistrationFileId")] public required File RegistrationFile { get; set; }

    [Required] [StringLength(450)] public required string LocationId { get; set; }
    [ForeignKey("LocationId")] public Location Location { get; set; } = null!;

    [Required] [StringLength(450)] public required string MajorId { get; set; }
    [ForeignKey("MajorId")] public Major Major { get; set; } = null!;

    // For tracking
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime Updated { get; set; } = DateTime.UtcNow;
}

public enum CompanySize
{
    Tiny,
    Small,
    Middle,
    Large,
    Enterprise
}