using System.ComponentModel.DataAnnotations;

namespace ProjectX.DTOs;

public class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public required string Email { set; get; }
}