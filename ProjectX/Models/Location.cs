using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectX.Models;

public class Location
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public required string Id { get; set; }

    [Required] [StringLength(50)] public required string Name { get; set; }
    public Region Region { get; set; } = Region.North;

    // Relationship
    public ICollection<CompanyDetail> Companies { get; set; } = new List<CompanyDetail>();
    
    // Tracking
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime Updated { get; set; } = DateTime.UtcNow;
}

public enum Region
{
    North,
    Central,
    South
}