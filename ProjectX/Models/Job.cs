using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProjectX.Data;

namespace ProjectX.Models;

public class Job : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [StringLength(150)] public required string Title { get; set; }
    [StringLength(2000)] public required string Description { get; set; }
    [StringLength(256)] public required string OfficeAddress { get; set; }
    public int Quantity { get; set; } = 1;
    [Column(TypeName = "nvarchar(50)")] public JobStatus Status { get; set; } = JobStatus.Active;
    [Column(TypeName = "nvarchar(50)")] public EducationLevel? Level { get; set; } = EducationLevel.University;
    public double? YearOfExperience { get; set; }
    [Range(0, double.MaxValue)] public double? MinSalary { get; set; }
    [Range(0, double.MaxValue)] public double? MaxSalary { get; set; }

    // Relationship
    [Required] public Guid MajorId { get; set; }
    [ForeignKey("MajorId")] public Major Major { get; set; } = null!;
    [Required] public Guid CampaignId { get; set; }
    [ForeignKey("CampaignId")] public Campaign Campaign { get; set; } = null!;
    [Required] public Guid LocationId { get; set; }
    [ForeignKey("LocationId")] public Location Location { get; set; } = null!;
    public ICollection<Skill> Skills { get; set; } = new List<Skill>();
    public ICollection<ContractType> ContractTypes { get; set; } = new List<ContractType>();
    public ICollection<JobLevel> JobLevels { get; set; } = new List<JobLevel>();
    public ICollection<JobType> JobTypes { get; set; } = new List<JobType>();

    public byte IsHighlight { get; set; } = 0;
    public DateTime? HighlightStart { get; set; }
    public DateTime? HighlightEnd { get; set; }

    public ICollection<Application> Applications { get; set; } = new List<Application>();

    // Tracking
    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
}

public enum JobStatus
{
    Draft,
    Active,
    Closed
}