using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ProjectX.Data;

namespace ProjectX.Models;

public class CompanyDetail : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] [StringLength(450)] public required string CompanyName { get; set; }

    [StringLength(10)] public required string ShortName { get; set; }

    [Required] [StringLength(10)] public required string TaxCode { get; set; }

    [Required] [StringLength(256)] public required string HeadQuarterAddress { get; set; }

    [Required] [StringLength(256)] public required string Logo { get; set; }
    [StringLength(256)] public string? Cover { get; set; } = "images/default-cover.png";

    [Required]
    [EmailAddress]
    [StringLength(50)]
    public required string ContactEmail { get; set; }

    [Required] [Phone] [StringLength(10)] public required string ContactPhone { get; set; }

    [StringLength(256)] public string? Website { get; set; }

    [Range(1900, 2100)] public required int FoundedYear { get; set; }

    [Column(TypeName = "nvarchar(50)")] public CompanySize Size { get; set; } = CompanySize.Tiny;

    [Required] [StringLength(10000)] public required string Introduction { get; set; }

    [Required]
    [Column(TypeName = "nvarchar(50)")]
    public VerifyStatus Status { get; set; } = VerifyStatus.Pending;

    [StringLength(500)] public string? RejectReason { get; set; }
    [Required] public Guid CompanyId { get; set; }

    [InverseProperty(nameof(User.CompanyDetail))]
    [JsonIgnore]
    [ForeignKey("CompanyId")]
    public User Company { get; set; } = null!;

    [Required] public required Guid LocationId { get; set; }

    [JsonIgnore]
    [ForeignKey("LocationId")]
    public Location Location { get; set; } = null!;

    [JsonIgnore] public ICollection<Major> Majors { get; set; } = new List<Major>();
    public bool IsPro { get; set; }
    [JsonIgnore] public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    public double AvgRatings { get; set; } = 0;

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
}

public enum VerifyStatus
{
    Pending,
    Verified,
    Rejected
}

public enum CompanySize
{
    Tiny,
    Small,
    Middle,
    Large,
    Enterprise
}