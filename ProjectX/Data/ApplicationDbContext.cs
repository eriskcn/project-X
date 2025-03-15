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
                    {
                        if (!softDeleteEntity.IsDeleted)
                        {
                            softDeleteEntity.Deleted = null;
                        }

                        break;
                    }
                }
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ConfigureSoftDelete();

        builder.Entity<Role>().HasData(
            new Role { Id = Guid.NewGuid(), Name = "Admin", NormalizedName = "ADMIN" },
            new Role { Id = Guid.NewGuid(), Name = "Candidate", NormalizedName = "CANDIDATE" },
            new Role { Id = Guid.NewGuid(), Name = "Business", NormalizedName = "BUSINESS" },
            new Role { Id = Guid.NewGuid(), Name = "FreelanceRecruiter", NormalizedName = "FREELANCERECRUITER" }
        );

        builder.Entity<Location>().HasData(
            new Location { Id = Guid.NewGuid(), Name = "Hà Nội", Region = Region.North },
            new Location { Id = Guid.NewGuid(), Name = "Đà Nẵng", Region = Region.Central },
            new Location { Id = Guid.NewGuid(), Name = "Hồ Chí Minh", Region = Region.South }
        );

        builder.Entity<JobType>().HasData(
            new JobType { Id = Guid.NewGuid(), Name = "In-Office" },
            new JobType { Id = Guid.NewGuid(), Name = "Remote" },
            new JobType { Id = Guid.NewGuid(), Name = "Hybrid" },
            new JobType { Id = Guid.NewGuid(), Name = "Oversea" }
        );

        builder.Entity<JobLevel>().HasData(
            new JobLevel { Id = Guid.NewGuid(), Name = "Intern" },
            new JobLevel { Id = Guid.NewGuid(), Name = "Junior" },
            new JobLevel { Id = Guid.NewGuid(), Name = "Senior" },
            new JobLevel { Id = Guid.NewGuid(), Name = "Lead" },
            new JobLevel { Id = Guid.NewGuid(), Name = "Manager" },
            new JobLevel { Id = Guid.NewGuid(), Name = "Director" }
        );

        builder.Entity<ContractType>().HasData(
            new ContractType { Id = Guid.NewGuid(), Name = "Part-time" },
            new ContractType { Id = Guid.NewGuid(), Name = "Full-time" },
            new ContractType { Id = Guid.NewGuid(), Name = "Freelance" }
        );

        builder.Entity<Major>().HasData(
            new Major { Id = Guid.NewGuid(), Name = "Công nghệ Phần mềm" },
            new Major { Id = Guid.NewGuid(), Name = "Khoa học Dữ liệu" },
            new Major { Id = Guid.NewGuid(), Name = "Khoa học Máy tính" }
        );
    }
}