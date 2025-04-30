using System.ComponentModel.DataAnnotations;
using ProjectX.Models;

namespace ProjectX.DTOs;

public class UpdateApplicationRequest
{
    [StringLength(100)] public string? FullName { get; set; }
    [EmailAddress] [StringLength(100)] public string? Email { get; set; }
    [Phone] [StringLength(10)] public string? PhoneNumber { get; set; }
    [StringLength(10000)] public string? Introduction { get; set; }
    public ApplicationStatus? Status { get; set; } = ApplicationStatus.Submitted;
    public IFormFile? Resume { get; set; }
}