using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using ProjectX.Data;

namespace ProjectX.Models;

public class Application : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [StringLength(70)] public required string FullName { get; set; }
    [EmailAddress] [StringLength(70)] public required string Email { get; set; }
    [Required] [Phone] [StringLength(10)] public required string PhoneNumber { get; set; }

    [StringLength(600)] public string? Introduction { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime Applied { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "nvarchar(50)")] public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;
    [Column(TypeName = "nvarchar(50)")] public ApplicationProcess Process { get; set; } = ApplicationProcess.Pending;

    [Required] public Guid CandidateId { get; set; }

    [JsonIgnore]
    [ForeignKey("CandidateId")]
    public User Candidate { get; set; } = null!;

    [Required] public Guid JobId { get; set; }
    [JsonIgnore] [ForeignKey("JobId")] public Job Job { get; set; } = null!;

    public DateTime? Submitted { get; set; }

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
}

public enum ApplicationProcess
{
    Pending,
    Shortlisted,
    Interviewing,
    Offered,
    Hired,
    Rejected
}

public enum ApplicationStatus
{
    Draft,
    Submitted,
    Seen
}