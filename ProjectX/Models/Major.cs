using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ProjectX.Data;

namespace ProjectX.Models;

public class Major : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] [StringLength(100)] public required string Name { get; set; }

    [JsonIgnore] public ICollection<Job> Jobs { get; set; } = new List<Job>();

    [JsonIgnore]
    [InverseProperty(nameof(User.FocusMajors))]
    public ICollection<User> Users { get; set; } = new List<User>();

    [JsonIgnore] public ICollection<CompanyDetail> Companies { get; set; } = new List<CompanyDetail>();

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
}