using System.ComponentModel.DataAnnotations;

namespace ProjectX.DTOs;

public class SignInRequest
{
    [EmailAddress] [StringLength(70)] public required string Email { get; set; }
    public required string Password { get; set; }
}