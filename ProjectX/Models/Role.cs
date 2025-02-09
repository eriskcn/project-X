using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace ProjectX.Models;

// 4 roles: Admin, Candidate, Company, FreelanceRecruiter
public class Role : IdentityRole
{
    // Attributes from IdentityRole
    // string Id
    // string? Name
    // string? NormalizedName
    // string? ConcurrencyStamp

    // For relationship
    public ICollection<User> Users { get; set; } = new List<User>();

    // For tracking
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}