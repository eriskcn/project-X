using System.ComponentModel.DataAnnotations;

namespace ProjectX.DTOs;

public class SignUpRequest
{
    [Required] [StringLength(70)] public required string FullName { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(70)]
    public required string Email { get; set; }

    public required string Password { get; set; }
    public required string RoleName { get; set; }
}