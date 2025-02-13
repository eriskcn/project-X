namespace ProjectX.DTOs;

public class SignUpRequest
{
    public required string FirstName { get; set; }
    public string? MiddleName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string RoleName { get; set; }
    public string? GitHubProfile { get; set; }
    public string? LinkedInProfile { get; set; }
}