using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ProjectX.Data;

namespace ProjectX.Models;

public class Appointment : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    [Required] public required Guid ApplicationId { get; set; }

    [ForeignKey("ApplicationId")]
    [JsonIgnore]
    [InverseProperty(nameof(Application.Appointment))]
    public Application Application { get; set; } = null!;

    [StringLength(600)] public string? Note { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}