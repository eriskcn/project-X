using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectX.Models;

public class Application
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public required string Id { get; set; }

    [StringLength(600)] public string? Introduction { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime Applied { get; set; } = DateTime.UtcNow;

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;
    public ApplicationProcess Process { get; set; } = ApplicationProcess.Pending;

    [StringLength(450)] public required string CandidateId { get; set; }
    [ForeignKey("CandidateId")] public User Candidate { get; set; } = null!;

    [StringLength(450)] public required string JobId { get; set; }
    [ForeignKey("JobId")] public Job Job { get; set; } = null!;

    [StringLength(450)] public required string CvFileId { get; set; }
    [ForeignKey("CvFileId")] public File CvFile { get; set; } = null!;

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
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