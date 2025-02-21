namespace ProjectX.DTOs;

public class SignUpRequest
{
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string RoleName { get; set; }
    public string? GitHubProfile { get; set; }
    public string? LinkedInProfile { get; set; }
}