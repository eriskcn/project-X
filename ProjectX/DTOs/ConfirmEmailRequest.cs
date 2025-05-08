using System.ComponentModel.DataAnnotations;

namespace ProjectX.DTOs;

public class ConfirmEmailRequest
{
    [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must consist of exactly 6 digits. ")]
    public required string Otp { get; set; }
}