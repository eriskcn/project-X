using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace ProjectX.Models;

public class User : IdentityUser
{
    // Attributes from IdentityUser
    // string Id  
    // string? Username
    // string? NormalizedUserName
    // string? Email
    // string? NormalizedEmail
    // bool EmailConfirmed
    // string? PasswordHash
    // string? SecurityStamp
    // string ? ConcurrencyStamp
    // string? PhoneNumber
    // bool PhoneNumberConfirmed
    // bool TwoFactorEnabled
    // DateTimeOffset? LockoutEnd
    // bool LockoutEnabled
    // int AccessFailedCount

    // Name
    [StringLength(20)] public required string FirstName { get; set; }

    [StringLength(20)] public string? MiddleName { get; set; }

    [StringLength(20)] public required string LastName { get; set; }

    [StringLength(70)] public required string FullName { get; set; }

    // Profile
    [StringLength(256)] public required string ProfilePicture { get; set; }
    [StringLength(100)] public string? GitHubProfile { get; set; }
    [StringLength(150)] public string? LinkedInProfile { get; set; }

    // Business
    public bool BusinessVerified { get; set; } = false;
    public double BusinessPoints { get; set; } = 0;

    // Relationship
    [StringLength(450)] public required string RoleId { get; set; }
    [ForeignKey("RoleId")] public required Role Role { get; set; }

    public ICollection<Skill> Skills { get; set; } = new List<Skill>();
    
    public ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public ICollection<Major> FocusMajors { get; set; } = new List<Major>();

    // Authentication
    [StringLength(64)] public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiry { get; set; } = DateTime.UtcNow.AddDays(7);

    // Tracking
    public UserStatus Status { get; set; } = UserStatus.Online;
    public required DateTime LassAccess { get; set; } = DateTime.UtcNow;
    public int LoginAttempts { get; set; } = 0;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}

public enum UserStatus
{
    Online,
    Offline,
    Busy
}