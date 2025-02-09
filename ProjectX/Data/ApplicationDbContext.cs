using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectX.Models;

namespace ProjectX.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<User, Role, string>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        foreach (var relationship in builder.Model.GetEntityTypes()
                     .SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }

        builder.Entity<Role>().HasData(
            new Role 
            { 
                Id = "1", 
                Name = "Admin", 
                NormalizedName = "ADMIN",
                ConcurrencyStamp = Guid.NewGuid().ToString()
            },
            new Role 
            { 
                Id = "2", 
                Name = "Candidate", 
                NormalizedName = "CANDIDATE",
                ConcurrencyStamp = Guid.NewGuid().ToString()
            },
            new Role 
            { 
                Id = "3", 
                Name = "Business", 
                NormalizedName = "BUSINESS",
                ConcurrencyStamp = Guid.NewGuid().ToString()
            },
            new Role 
            { 
                Id = "4", 
                Name = "FreelanceRecruiter", 
                NormalizedName = "FREELANCERECRUITER", 
                ConcurrencyStamp = Guid.NewGuid().ToString()
            }
        );
    }
}