using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectX.Models;

public class Education
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public required string Id { get; set; }

    public EducationLevel Level { get; set; } = EducationLevel.PrimarySchool;
    public required string School { get; set; }
    [Range(1900, 2100)] public int StartYear { get; set; }
    public bool Graduated { get; set; } = false;
    [Range(1900, 2100)] public int? GraduatedYear { get; set; }

    // Relationship
    [StringLength(450)] public required string CandidateId { get; set; }
    [ForeignKey("CandidateId")] public User Candidate { get; set; } = null!;

    [StringLength(450)] public string? MajorId { get; set; }
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