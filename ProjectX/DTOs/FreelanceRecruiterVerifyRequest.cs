namespace ProjectX.DTOs;

public class FreelanceRecruiterVerifyRequest
{
    public required IFormFile FrontIdCard { get; set; } = null!;
    public required IFormFile BackIdCard { get; set; } = null!;
}