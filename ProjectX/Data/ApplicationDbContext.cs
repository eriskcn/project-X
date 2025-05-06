using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectX.Models;

namespace ProjectX.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<User, Role, Guid>(options)
{
    public DbSet<Application> Applications { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<AttachedFile> AttachedFiles { get; set; }
    public DbSet<Campaign> Campaigns { get; set; }
    public DbSet<CompanyDetail> CompanyDetails { get; set; }
    public DbSet<FreelanceRecruiterDetail> FreelanceRecruiterDetails { get; set; }
    public DbSet<ContractType> ContractTypes { get; set; }
    public DbSet<Education> Educations { get; set; }
    public DbSet<Job> Jobs { get; set; }
    public DbSet<JobLevel> JobLevels { get; set; }
    public DbSet<JobType> JobTypes { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Major> Majors { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Like> Likes { get; set; }
    public DbSet<Skill> Skills { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<JobService> JobServices { get; set; }
    public DbSet<BusinessPackage> BusinessPackages { get; set; }
    public DbSet<PurchasedPackage> PurchasedPackages { get; set; }
    public DbSet<PackageUpdate> PackageUpdates { get; set; }

    public DbSet<Payment> Payments { get; set; }

    public DbSet<Notification> Notifications { get; set; }

    public DbSet<TokenTransaction> TokenTransactions { get; set; }
    public DbSet<Rating> Ratings { get; set; }

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

        builder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        builder.Entity<Role>()
            .HasIndex(r => r.Name)
            .IsUnique();

        builder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Job>()
            .HasMany(j => j.Skills)
            .WithMany(s => s.Jobs)
            .UsingEntity(j => j.ToTable("JobSkills"));

        builder.Entity<Job>()
            .HasMany(j => j.JobLevels)
            .WithMany(jl => jl.Jobs)
            .UsingEntity(j => j.ToTable("JobJobLevels"));

        builder.Entity<Job>()
            .HasMany(j => j.ContractTypes)
            .WithMany(ct => ct.Jobs)
            .UsingEntity(j => j.ToTable("JobContractTypes"));

        builder.Entity<Job>()
            .HasMany(j => j.JobTypes)
            .WithMany(jt => jt.Jobs)
            .UsingEntity(j => j.ToTable("JobJobTypes"));

        builder.Entity<User>()
            .HasMany(u => u.FocusMajors)
            .WithMany(m => m.Users)
            .UsingEntity(u => u.ToTable("UserFocusMajors"));

        builder.Entity<CompanyDetail>()
            .HasMany(cd => cd.Majors)
            .WithMany(m => m.Companies)
            .UsingEntity(cd => cd.ToTable("CompanyDetailMajors"));

        builder.Entity<CompanyDetail>()
            .HasMany(cd => cd.Ratings)
            .WithOne(r => r.Company);

        builder.Entity<User>()
            .HasMany(cd => cd.Ratings)
            .WithOne(r => r.Candidate);

        builder.Entity<User>()
            .HasOne(u => u.CompanyDetail)
            .WithOne(cd => cd.Company)
            .HasForeignKey<CompanyDetail>(cd => cd.CompanyId)
            .IsRequired();

        builder.Entity<User>()
            .HasMany(u => u.SavedJobs)
            .WithMany(j => j.SavedByUsers)
            .UsingEntity(u => u.ToTable("UserSavedJobs"));

        builder.Entity<Location>()
            .HasMany(l => l.Companies)
            .WithOne(cd => cd.Location)
            .HasForeignKey(cd => cd.LocationId)
            .IsRequired();

        builder.Entity<Post>().HasIndex(p => p.Content);
        builder.Entity<Post>().HasIndex(p => p.Created);
        builder.Entity<Post>().HasIndex(p => p.ParentId);

        builder.Entity<Like>().HasIndex(l => l.PostId);
        builder.Entity<Like>().HasIndex(l => l.UserId);

        builder.Entity<Like>()
            .HasIndex(l => new { l.UserId, l.PostId })
            .IsUnique();

        builder.Entity<Appointment>()
            .HasOne(a => a.Application)
            .WithOne(a => a.Appointment);

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

        builder.Entity<Skill>().HasData(
            new Skill { Id = Guid.NewGuid(), Name = "ASP.NET Core" },
            new Skill { Id = Guid.NewGuid(), Name = "C#" },
            new Skill { Id = Guid.NewGuid(), Name = "JavaScript" },
            new Skill { Id = Guid.NewGuid(), Name = "Nextjs" },
            new Skill { Id = Guid.NewGuid(), Name = "Angular" }
        );

        builder.Entity<Service>().HasData(
            new Service
            {
                Id = Guid.NewGuid(),
                Name = "Mark as highlight",
                Type = ServiceType.Highlight,
                Description =
                    "Hiển thị tin tại khu vực việc làm nổi bật (tối đa 14 ngày). 5 X Token/ngày hoặc 10.000đ/ngày (7 ngày đầu), 12.000đ/ngày từ ngày thứ 8.",
                DayLimit = 14,
                CashPrice = 10_000,
                XTokenPrice = 5
            },
            new Service
            {
                Id = Guid.NewGuid(),
                Name = "Mark as urgent",
                Type = ServiceType.Urgent,
                Description =
                    "Gắn thẻ Urgent cho tin tuyển dụng (tối đa 7 ngày). Hỗ trợ lọc bằng bộ lọc Urgent. Giá: 20 X Token hoặc 40.000đ/tin.",
                DayLimit = 7,
                CashPrice = 40_000,
                XTokenPrice = 20
            },
            new Service
            {
                Id = Guid.NewGuid(),
                Name = "Mark as hot",
                Description =
                    "Gắn thẻ Hot cho tin tuyển dụng (tối đa 14 ngày). Hỗ trợ lọc bằng bộ lọc Hot. Giá: 20 X Token hoặc 40.000đ/tin.",
                DayLimit = 14,
                Type = ServiceType.Hot,
                CashPrice = 40_000,
                XTokenPrice = 20
            }
        );

        builder.Entity<BusinessPackage>().HasData(
            new BusinessPackage
            {
                Id = Guid.NewGuid(),
                Name = "Project X Premium - 1 month",
                Description = "Basic package for businesses",
                Level = BusinessLevel.Premium,
                CashPrice = 249_000,
                DurationInDays = 30,
                MonthlyXTokenRewards = 100
            },
            new BusinessPackage
            {
                Id = Guid.NewGuid(),
                Name = "Project X Premium - 3 months",
                Description = "Basic package for businesses",
                Level = BusinessLevel.Premium,
                CashPrice = 690_000,
                DurationInDays = 90,
                MonthlyXTokenRewards = 100
            },
            new BusinessPackage
            {
                Id = Guid.NewGuid(),
                Name = "Project X Premium - 6 months",
                Description = "Basic package for businesses",
                Level = BusinessLevel.Premium,
                CashPrice = 1_290_000,
                DurationInDays = 180,
                MonthlyXTokenRewards = 100
            },
            new BusinessPackage
            {
                Id = Guid.NewGuid(),
                Name = "Project X Premium - 12 months",
                Description = "Basic package for businesses",
                Level = BusinessLevel.Premium,
                CashPrice = 2_490_000,
                DurationInDays = 360,
                MonthlyXTokenRewards = 100
            },
            new BusinessPackage
            {
                Id = Guid.NewGuid(),
                Name = "Project X Elite - 1 month",
                Description = "Elite package for businesses",
                Level = BusinessLevel.Elite,
                CashPrice = 499_000,
                DurationInDays = 30,
                MonthlyXTokenRewards = 500
            },
            new BusinessPackage
            {
                Id = Guid.NewGuid(),
                Name = "Project X Elite - 3 months",
                Description = "Elite package for businesses",
                Level = BusinessLevel.Elite,
                CashPrice = 1_390_000,
                DurationInDays = 90,
                MonthlyXTokenRewards = 500
            },
            new BusinessPackage
            {
                Id = Guid.NewGuid(),
                Name = "Project X Elite - 6 months",
                Description = "Elite package for businesses",
                Level = BusinessLevel.Elite,
                CashPrice = 2_490_000,
                DurationInDays = 180,
                MonthlyXTokenRewards = 500
            },
            new BusinessPackage
            {
                Id = Guid.NewGuid(),
                Name = "Project X Elite - 12 months",
                Description = "Elite package for businesses",
                Level = BusinessLevel.Elite,
                CashPrice = 4_990_000,
                DurationInDays = 360,
                MonthlyXTokenRewards = 500
            },
            new BusinessPackage
            {
                Id = Guid.NewGuid(),
                Name = "Test package",
                Description = "hehe",
                Level = BusinessLevel.Elite,
                CashPrice = 2_000,
                DurationInDays = 360,
                MonthlyXTokenRewards = 500
            }
        );
    }
}