using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectX.Models;

namespace ProjectX.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<User, Role, Guid>(options)
{
    public DbSet<Application> Applications { get; set; }
    public DbSet<AttachedFile> AttachedFiles { get; set; }
    public DbSet<Campaign> Campaigns { get; set; }
    public DbSet<CompanyDetail> CompanyDetails { get; set; }
    public DbSet<ContractType> ContractTypes { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Education> Educations { get; set; }
    public DbSet<Job> Jobs { get; set; }
    public DbSet<JobLevel> JobLevels { get; set; }
    public DbSet<JobType> JobTypes { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Major> Majors { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Skill> Skills { get; set; }

    public override int SaveChanges()
    {
        UpdateSoftDeleteStatuses();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateSoftDeleteStatuses();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateSoftDeleteStatuses()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is ISoftDelete softDeleteEntity)
            {
                switch (entry.State)
                {
                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        softDeleteEntity.IsDeleted = true;
                        softDeleteEntity.Deleted = DateTime.UtcNow;
                        break;

                    case EntityState.Modified:
                        if (!softDeleteEntity.IsDeleted)
                        {
                            softDeleteEntity.Deleted = null;
                        }

                        break;
                }
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ConfigureSoftDelete();

        builder.Entity<Role>().Property(r => r.Modified).HasDefaultValueSql("GETUTCDATE()");
        builder.Entity<Role>().HasData(
            new Role
            {
                Id = Guid.NewGuid(),
                Name = "Admin",
                NormalizedName = "ADMIN",
                IsDeleted = false,
                Modified = DateTime.UtcNow
            },
            new Role
            {
                Id = Guid.NewGuid(),
                Name = "Candidate",
                NormalizedName = "CANDIDATE",
                IsDeleted = false,
                Modified = DateTime.UtcNow
            },
            new Role
            {
                Id = Guid.NewGuid(),
                Name = "Business",
                NormalizedName = "BUSINESS",
                IsDeleted = false,
                Modified = DateTime.UtcNow
            },
            new Role
            {
                Id = Guid.NewGuid(),
                Name = "FreelanceRecruiter",
                NormalizedName = "FREELANCERECRUITER",
                IsDeleted = false,
                Modified = DateTime.UtcNow
            }
        );
    }
}