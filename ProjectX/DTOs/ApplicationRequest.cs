using System.ComponentModel.DataAnnotations;
using ProjectX.Models;

namespace ProjectX.DTOs;

public class ApplicationRequest
{
    [Required]
    [StringLength(100)]
    public required string FullName { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public required string Email { get; set; }

    [Required]
    [Phone]
    [StringLength(15)]
    public required string PhoneNumber { get; set; }

    [StringLength(10000)]
    public string? Introduction { get; set; }

    [Required]
    public required ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;

    [Required]
    public required IFormFile Resume { get; set; }
}