using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using ProjectX.Data;

namespace ProjectX.Models;

public class Location : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] [StringLength(50)] public required string Name { get; set; }
    [Column(TypeName = "nvarchar(50)")] public Region Region { get; set; } = Region.North;

    [JsonIgnore] public ICollection<CompanyDetail> Companies { get; set; } = new List<CompanyDetail>();
    [JsonIgnore] public ICollection<Job> Jobs { get; set; } = new List<Job>();

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
}

public enum Region
{
    North,
    Central,
    South
}