using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ProjectX.Controllers;
using ProjectX.Data;

namespace ProjectX.Models;

public class Job : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [StringLength(150)] public required string Title { get; set; }
    [StringLength(10000)] public required string Description { get; set; }
    public int ViewCount { get; set; }
    [StringLength(256)] public required string OfficeAddress { get; set; }
    public int Quantity { get; set; } = 1;
    [Column(TypeName = "nvarchar(50)")] public JobStatus Status { get; set; } = JobStatus.Pending;
    [StringLength(500)] public string? RejectReason { get; set; }

    [Column(TypeName = "nvarchar(50)")]
    public EducationLevel? EducationLevelRequire { get; set; } = EducationLevel.University;

    public double? YearOfExperience { get; set; }
    [Range(0, double.MaxValue)] public double? MinSalary { get; set; }
    [Range(0, double.MaxValue)] public double? MaxSalary { get; set; }

    [Required] public Guid MajorId { get; set; }

    [JsonIgnore] [ForeignKey("MajorId")] public Major Major { get; set; } = null!;
    [Required] public Guid CampaignId { get; set; }

    [JsonIgnore]
    [ForeignKey("CampaignId")]
    public Campaign Campaign { get; set; } = null!;

    [Required] public Guid LocationId { get; set; }

    [JsonIgnore]
    [ForeignKey("LocationId")]
    public Location Location { get; set; } = null!;

    public bool IsHighlight { get; set; }
    public bool IsUrgent { get; set; }
    public bool IsHot { get; set; }
    public JobPaymentMethod PaymentMethod { get; set; } = JobPaymentMethod.XToken;

    [JsonIgnore]
    [InverseProperty(nameof(Skill.Jobs))]
    public ICollection<Skill> Skills { get; set; } = new List<Skill>();

    [JsonIgnore]
    [InverseProperty(nameof(User.SavedJobs))]
    public ICollection<User> SavedByUsers { get; set; } = new List<User>();

    [JsonIgnore]
    [InverseProperty(nameof(ContractType.Jobs))]
    public ICollection<ContractType> ContractTypes { get; set; } = new List<ContractType>();

    // n-n relationship
    [InverseProperty(nameof(JobLevel.Jobs))]
    [JsonIgnore]
    public ICollection<JobLevel> JobLevels { get; set; } = new List<JobLevel>();

    // n-n relationship
    [InverseProperty(nameof(JobType.Jobs))]
    [JsonIgnore]
    public ICollection<JobType> JobTypes { get; set; } = new List<JobType>();

    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime EndDate { get; set; } = DateTime.UtcNow.AddDays(7);

    [JsonIgnore] public ICollection<Application> Applications { get; set; } = new List<Application>();
    [JsonIgnore] public ICollection<JobService> JobServices { get; set; } = new List<JobService>();
    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
}

public enum JobStatus
{
    Draft,
    Pending,
    Rejected,
    Active,
    Closed
}

public enum JobPaymentMethod
{
    XToken,
    Cash
}