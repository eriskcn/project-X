using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProjectX.Data;

namespace ProjectX.Models;

public class Major : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] [StringLength(100)] public required string Name { get; set; }

    // Relationship
    public ICollection<Job> Jobs { get; set; } = new List<Job>();
    [InverseProperty("FocusMajors")]
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<CompanyDetail> Companies { get; set; } = new List<CompanyDetail>();

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
}