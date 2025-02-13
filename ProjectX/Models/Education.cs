using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProjectX.Data;

namespace ProjectX.Models;

public class Education : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    public EducationLevel Level { get; set; } = EducationLevel.PrimarySchool;

    [StringLength(200)] public required string School { get; set; }

    [Range(1900, 2100)] public int StartYear { get; set; }

    public bool Graduated { get; set; }

    [Range(1900, 2100)] public int? GraduatedYear { get; set; }

    // Relationship
    [Required] public Guid CandidateId { get; set; }
    [ForeignKey("CandidateId")] public User Candidate { get; set; } = null!;

    public Guid? MajorId { get; set; }
    [ForeignKey("MajorId")] public Major? Major { get; set; }

    // Tracking
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
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