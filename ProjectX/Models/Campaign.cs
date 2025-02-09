using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectX.Models;

public class Campaign
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public required string Id { get; set; }

    [Required] [StringLength(256)] public required string Name { get; set; }
    [Required] [StringLength(1000)] public required string Description { get; set; }
    [Required] public DateTime Open { get; set; } = DateTime.UtcNow;
    [Required] public DateTime Close { get; set; } = DateTime.UtcNow.AddDays(7);
    public bool IsHighlight { get; set; } = false;
    public bool IsUrgent { get; set; } = false;

    // Relationship
    public required string RecruiterId { get; set; }
    [ForeignKey("RecruiterId")] public User Recruiter { get; set; } = null!;

    // Tracking
    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}

public enum CampaignStatus
{
    Draft,
    Opened,
    Closed
}