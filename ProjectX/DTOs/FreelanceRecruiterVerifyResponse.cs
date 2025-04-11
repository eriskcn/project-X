namespace ProjectX.DTOs;

public class FreelanceRecruiterVerifyResponse
{
    public Guid UserId { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ProfilePicture { get; set; }
    public string? GitHubProfile { get; set; }
    public string? LinkedInProfile { get; set; }
    public FreelanceRecruiterDetailResponse FreelanceRecruiter { get; set; } = null!;
    public bool FreelanceRecruiterVerified { get; set; }
}