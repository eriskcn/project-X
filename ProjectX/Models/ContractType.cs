using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProjectX.Data;

namespace ProjectX.Models;

// Part-time, Full-time, Freelance
public class ContractType : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] 
    [StringLength(50)] 
    public required string Name { get; set; }

    // Relationship
    public ICollection<Job> Jobs { get; set; } = new List<Job>();

    // Tracking
    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
}