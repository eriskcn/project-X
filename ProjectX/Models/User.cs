using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using ProjectX.Data;

namespace ProjectX.Models;

public class User : IdentityUser<Guid>, ISoftDelete
{
    // Name
    [Required] [StringLength(70)] public required string FullName { get; set; }

    // Profile
    [StringLength(256)] public string ProfilePicture { get; set; } = "/images/default-avatar.jpeg";
    [StringLength(100)] public string? GitHubProfile { get; set; }
    [StringLength(150)] public string? LinkedInProfile { get; set; }

    // Social Authentication
    [StringLength(50)] public string? Provider { get; set; } 
    [StringLength(100)] public string? OAuthId { get; set; } 
    public bool IsExternalLogin => !string.IsNullOrEmpty(Provider); 

    // Business
    public bool BusinessVerified { get; set; } = false;
    public double BusinessPoints { get; set; } = 0;
    
    public bool IsDeleted { get; set; }
    public DateTime? Deleted { get; set; }

    // Relationship
    public CompanyDetail? CompanyDetail { get; set; }
    public ICollection<Skill> Skills { get; set; } = new List<Skill>();
    public ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    [InverseProperty("Users")] public ICollection<Major> FocusMajors { get; set; } = new List<Major>();
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Post> LikedPosts { get; set; } = new List<Post>();
    public ICollection<Post> DislikedPosts { get; set; } = new List<Post>();

    // Authentication
    [StringLength(64)] public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiry { get; set; } = DateTime.UtcNow.AddDays(7);

    // Tracking
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