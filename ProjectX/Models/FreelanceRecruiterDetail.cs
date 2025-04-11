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

    [Required] public Guid FreelanceRecruiterId { get; set; }

    [JsonIgnore]
    [ForeignKey("FreelanceRecruiterId")]
    public User FreelanceRecruiter { get; set; } = null!;
}