using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using ProjectX.Data;

namespace ProjectX.Models;

// 4 roles: Admin, Candidate, Company, FreelanceRecruiter
public class Role : IdentityRole<Guid>, ISoftDelete
{
    public bool IsDeleted { get; set; }

    public DateTime? Deleted { get; set; }

    // Tracking
    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
}