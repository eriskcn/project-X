using System.ComponentModel.DataAnnotations;

namespace ProjectX.DTOs;

public class SignInRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}