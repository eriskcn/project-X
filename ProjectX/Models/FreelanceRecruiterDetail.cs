using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ProjectX.Data;

namespace ProjectX.Models;

public class FreelanceRecruiterDetail : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    public VerifyStatus Status { get; set; } = VerifyStatus.Pending;
    [StringLength(500)] public string? RejectReason { get; set; }

    [Required] public Guid FreelanceRecruiterId { get; set; }

    [JsonIgnore]
    [ForeignKey("FreelanceRecruiterId")]
    public User FreelanceRecruiter { get; set; } = null!;
}