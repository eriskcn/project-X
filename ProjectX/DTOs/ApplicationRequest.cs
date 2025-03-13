using ProjectX.Models;

namespace ProjectX.DTOs;

public class ApplicationRequest
{
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string PhoneNumber { get; set; }
    public string? Introduction { get; set; }
    public required ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;
    public Guid JobId { get; set; }
    public required IFormFile Resume { get; set; }
}