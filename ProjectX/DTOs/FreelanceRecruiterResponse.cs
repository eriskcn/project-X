namespace ProjectX.DTOs;

public class FreelanceRecruiterResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ProfilePicture { get; set; } = string.Empty;
    public string LinkedInProfile { get; set; } = string.Empty;
    public string GitHubProfile { get; set; } = string.Empty;
}