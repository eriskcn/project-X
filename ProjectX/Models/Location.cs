using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProjectX.Data;

namespace ProjectX.Models;

public class Location : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] [StringLength(50)] public required string Name { get; set; }
    public Region Region { get; set; } = Region.North;

    // Relationship
    public ICollection<CompanyDetail> Companies { get; set; } = new List<CompanyDetail>();
    public ICollection<Job> Jobs { get; set; } = new List<Job>();

    // Tracking
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}

public enum Region
{
    North,
    Central,
    South
}