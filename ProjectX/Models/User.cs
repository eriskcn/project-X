using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using ProjectX.Data;

namespace ProjectX.Models;

public class User : IdentityUser<Guid>, ISoftDelete
{
    [Required] [StringLength(70)] public required string FullName { get; set; }

    [StringLength(256)] public string ProfilePicture { get; set; } = "/images/default-avatar.jpeg";
    [StringLength(100)] public string? GitHubProfile { get; set; }
    [StringLength(150)] public string? LinkedInProfile { get; set; }

    [StringLength(50)] public string? Provider { get; set; }
    [StringLength(100)] public string? OAuthId { get; set; }
    public bool IsExternalLogin => !string.IsNullOrEmpty(Provider);

    public bool RecruiterVerified { get; set; }
    public double XTokenBalance { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? Deleted { get; set; }

    [InverseProperty(nameof(CompanyDetail.Company))]
    [JsonIgnore]
    public CompanyDetail? CompanyDetail { get; set; }

    [JsonIgnore] public FreelanceRecruiterDetail? FreelanceRecruiterDetail { get; set; }

    [InverseProperty(nameof(Skill.Users))]
    [JsonIgnore]
    public ICollection<Skill> Skills { get; set; } = new List<Skill>();

    [InverseProperty(nameof(Job.SavedByUsers))]
    [JsonIgnore]
    public ICollection<Job> SavedJobs { get; set; } = new List<Job>();

    [JsonIgnore] public ICollection<Message> SentMessages { get; set; } = new List<Message>();
    [JsonIgnore] public ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
    [JsonIgnore] public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    [JsonIgnore] public ICollection<TokenTransaction> TokenTransactions { get; set; } = new List<TokenTransaction>();
    [JsonIgnore] public ICollection<Order> Orders { get; set; } = new List<Order>();
    [JsonIgnore] public ICollection<Rating>? Ratings { get; set; } = new List<Rating>();

    [JsonIgnore]
    [InverseProperty(nameof(Major.Users))]
    public ICollection<Major> FocusMajors { get; set; } = new List<Major>();

    [JsonIgnore] public ICollection<Post> Posts { get; set; } = new List<Post>();
    [JsonIgnore] public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    [StringLength(64)] public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiry { get; set; } = DateTime.UtcNow.AddDays(7);

    [StringLength(6)] public string? OTP { get; set; }
    public DateTime? OTPExpiry { get; set; } = DateTime.UtcNow.AddMinutes(5);

    public bool VerificationSubmitted { get; set; }
    [Column(TypeName = "nvarchar(50)")] public UserStatus Status { get; set; } = UserStatus.Online;
    public DateTime? LastAccess { get; set; } = DateTime.UtcNow;
    public int LoginAttempts { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}

public enum UserStatus
{
    Online,
    Offline,
    Busy
}