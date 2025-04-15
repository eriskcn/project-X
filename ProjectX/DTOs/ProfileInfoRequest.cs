using System.ComponentModel.DataAnnotations;

namespace ProjectX.DTOs;

public class ProfileInfoRequest
{
    [Required] [StringLength(70)] public string? FullName { get; set; }
    [Phone] [StringLength(10)] public string? PhoneNumber { get; set; }
    public IFormFile? ProfilePicture { get; set; }
    [StringLength(100)] public string? GitHubProfile { get; set; }
    [StringLength(150)] public string? LinkedInProfile { get; set; }
}