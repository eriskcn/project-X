using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProjectX.Data;

namespace ProjectX.Models;

public class Rating : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Range(0, 5)] public double Point { get; set; }

    public string? Comment { get; set; }
    public bool IsAnonymous { get; set; }

    public Guid CandidateId { get; set; }

    public Guid CompanyId { get; set; }

    [ForeignKey("CandidateId")] public User Candidate { get; set; } = null!;

    [ForeignKey("CompanyId")] public CompanyDetail Company { get; set; } = null!;

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
}