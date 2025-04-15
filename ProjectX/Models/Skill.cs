using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ProjectX.Data;

namespace ProjectX.Models;

public class Skill : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [StringLength(256)] public required string Name { get; set; }

    // n-n relationship
    [InverseProperty(nameof(Job.Skills))]
    [JsonIgnore]
    public ICollection<Job> Jobs { get; set; } = new List<Job>();

    // n-n relationship
    [InverseProperty(nameof(User.Skills))]
    [JsonIgnore]
    public ICollection<User> Users { get; set; } = new List<User>();

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
}