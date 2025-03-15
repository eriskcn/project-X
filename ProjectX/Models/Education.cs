using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using ProjectX.Data;

namespace ProjectX.Models;

public class Education : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column(TypeName = "nvarchar(50)")] public EducationLevel Level { get; set; } = EducationLevel.PrimarySchool;

    [StringLength(200)] public required string School { get; set; }

    [Range(1900, 2100)] public int StartYear { get; set; }

    public bool Graduated { get; set; }

    [Range(1900, 2100)] public int? GraduatedYear { get; set; }

    [Required] public Guid CandidateId { get; set; }

    [JsonIgnore]
    [ForeignKey("CandidateId")]
    public User Candidate { get; set; } = null!;

    public Guid? MajorId { get; set; }
    [JsonIgnore] [ForeignKey("MajorId")] public Major? Major { get; set; }

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
}

public enum EducationLevel
{
    PrimarySchool,
    SecondarySchool,
    HighSchool,
    College,
    University,
    Master,
    PhD
}