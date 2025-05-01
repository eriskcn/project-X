using ProjectX.Models;

namespace ProjectX.DTOs;

public class ProfileInfoResponse
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ProfilePicture { get; set; }
    public string? GitHubProfile { get; set; }
    public string? LinkedInProfile { get; set; }
    public ICollection<string> Roles { get; set; } = new List<string>();
    public bool VerificationSubmitted { get; set; }
    public bool RecruiterVerified { get; set; }
    public double XTokenBalance { get; set; }
    public bool IsElite { get; set; }
    public bool IsExternalLogin { get; set; }
    public string? Provider { get; set; }
    public UserStatus Status { get; set; }
}