using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using ProjectX.Data;

namespace ProjectX.Models;

// In-Office, Remote, Hybrid, Oversea
public class JobType : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] [StringLength(50)] public required string Name { get; set; }

    [JsonIgnore] public ICollection<Job> Jobs { get; set; } = new List<Job>();

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
}