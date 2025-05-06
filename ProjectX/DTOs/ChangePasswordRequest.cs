using System.ComponentModel.DataAnnotations;

namespace ProjectX.DTOs;

public class ChangePasswordRequest
{
    [Required]
    [StringLength(32, MinimumLength = 8)]
    public required string OldPassword { set; get; }

    [Required]
    [StringLength(32, MinimumLength = 8)]
    public required string NewPassword { set; get; }
}