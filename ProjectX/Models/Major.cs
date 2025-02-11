using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectX.Models;

public class Major
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public required string Id { get; set; }

    [Required] [StringLength(100)] public required string Name { get; set; }

    // Relationship
    public ICollection<Job> Jobs { get; set; } = new List<Job>();
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<CompanyDetail> Companies { get; set; } = new List<CompanyDetail>();

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime Created { get; set; } = DateTime.Now;

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime Modified { get; set; } = DateTime.Now;
}