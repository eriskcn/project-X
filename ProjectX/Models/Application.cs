using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProjectX.Data;

namespace ProjectX.Models;

public class Application : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [StringLength(600)] public string? Introduction { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime Applied { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "nvarchar(50)")] public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;
    [Column(TypeName = "nvarchar(50)")] public ApplicationProcess Process { get; set; } = ApplicationProcess.Pending;

    // Relationship - Candidate
    [Required] public Guid CandidateId { get; set; }
    [ForeignKey("CandidateId")] public User Candidate { get; set; } = null!;

    // Relationship - Job
    [Required] public Guid JobId { get; set; }
    [ForeignKey("JobId")] public Job Job { get; set; } = null!;

    // Relationship - CV File
    // [Required] public Guid CvFileId { get; set; }
    // [ForeignKey("CvFileId")] public AttachedFile CvAttachedFile { get; set; } = null!;
    public AttachedFile CvAttachedFile { get; set; } = null!;

    // Tracking
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