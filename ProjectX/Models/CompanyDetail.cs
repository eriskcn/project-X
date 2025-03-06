using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProjectX.Data;

namespace ProjectX.Models;

public class CompanyDetail : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [StringLength(450)] public required string Name { get; set; }

    [Required] [StringLength(10)] public string? ShortName { get; set; }

    [Required] [StringLength(256)] public required string HeadQuarterAddress { get; set; }

    [Required] [StringLength(256)] public required string Logo { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(50)]
    public required string ContactEmail { get; set; }

    [Range(1900, 2100)] public required int FoundedYear { get; set; }

    [Column(TypeName = "nvarchar(50)")] public CompanySize Size { get; set; } = CompanySize.Tiny;

    [Required] [StringLength(2500)] public required string Introduction { get; set; }

    // For relationship
    [Required] public Guid CompanyId { get; set; }
    [ForeignKey("CompanyId")] public User Company { get; set; } = null!;

    // [Required] public Guid RegistrationFileId { get; set; }
    // [ForeignKey("RegistrationFileId")] public required AttachedFile RegistrationAttachedFile { get; set; }

    [Required] public Guid LocationId { get; set; }
    [ForeignKey("LocationId")] public Location Location { get; set; } = null!;

    [Required] public Guid MajorId { get; set; }
    [ForeignKey("MajorId")] public Major Major { get; set; } = null!;

    // For tracking
    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
}

public enum CompanySize
{
    Tiny,
    Small,
    Middle,
    Large,
    Enterprise
}