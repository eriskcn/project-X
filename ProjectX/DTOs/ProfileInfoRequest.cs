namespace ProjectX.DTOs;

public class ProfileInfoRequest
{
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public IFormFile? ProfilePicture { get; set; }
    public string? GitHubProfile { get; set; }
    public string? LinkedInProfile { get; set; }
}