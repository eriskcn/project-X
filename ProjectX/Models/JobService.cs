using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ProjectX.Data;

namespace ProjectX.Models;

public class JobService : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid JobId { get; set; }
    [JsonIgnore] [ForeignKey("JobId")] public Job Job { get; set; } = null!;

    public Guid ServiceId { get; set; }
    [JsonIgnore] [ForeignKey("ServiceId")] public Service Service { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}