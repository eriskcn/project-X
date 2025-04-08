using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using ProjectX.Data;

namespace ProjectX.Models;

public class Campaign : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] [StringLength(256)] public required string Name { get; set; }

    [Required] [StringLength(1000)] public required string Description { get; set; }

    [Required] public DateTime Open { get; set; } = DateTime.UtcNow;

    [Required] public DateTime Close { get; set; } = DateTime.UtcNow.AddDays(7);
    // public int CountJobs { get; set; }

    [Required] public Guid RecruiterId { get; set; }

    [JsonIgnore]
    [ForeignKey("RecruiterId")]
    public User Recruiter { get; set; } = null!;

    [JsonIgnore] public ICollection<Job> Jobs { get; set; } = new List<Job>();
    [Column(TypeName = "nvarchar(50)")] public CampaignStatus Status { get; set; } = CampaignStatus.Draft;

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
}

public enum CampaignStatus
{
    Draft,
    Opened,
    Closed
}