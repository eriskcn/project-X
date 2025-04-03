using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
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

    [Column(TypeName = "nvarchar(50)")]
    public EducationLevel? EducationLevelRequire { get; set; } = EducationLevel.University;

    public double? YearOfExperience { get; set; }
    [Range(0, double.MaxValue)] public double? MinSalary { get; set; }
    [Range(0, double.MaxValue)] public double? MaxSalary { get; set; }

    [Required] public Guid MajorId { get; set; }

    // 1-n relationship
    [JsonIgnore] [ForeignKey("MajorId")] public Major Major { get; set; } = null!;
    [Required] public Guid CampaignId { get; set; }

    // 1-n relationship
    [JsonIgnore]
    [ForeignKey("CampaignId")]
    public Campaign Campaign { get; set; } = null!;

    [Required] public Guid LocationId { get; set; }

    // 1-n relationship
    [JsonIgnore]
    [ForeignKey("LocationId")]
    public Location Location { get; set; } = null!;

    // n-n relationship
    [InverseProperty(nameof(Skill.Jobs))]
    [JsonIgnore]
    public ICollection<Skill> Skills { get; set; } = new List<Skill>();

    // n-n relationship
    [InverseProperty(nameof(ContractType.Jobs))]
    [JsonIgnore]
    public ICollection<ContractType> ContractTypes { get; set; } = new List<ContractType>();

    // n-n relationship
    [InverseProperty(nameof(JobLevel.Jobs))]
    [JsonIgnore]
    public ICollection<JobLevel> JobLevels { get; set; } = new List<JobLevel>();

    // n-n relationship
    [InverseProperty(nameof(JobType.Jobs))]
    [JsonIgnore]
    public ICollection<JobType> JobTypes { get; set; } = new List<JobType>();

    public bool IsHighlight { get; set; }
    public DateTime? HighlightStart { get; set; }
    public DateTime? HighlightEnd { get; set; }

    // 1-n relationship
    [JsonIgnore] public ICollection<Application> Applications { get; set; } = new List<Application>();

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
}

public enum JobStatus
{
    Draft,
    Active,
    Closed
}