using System.ComponentModel.DataAnnotations;

namespace ProjectX.DTOs;

public class AppointmentRequest
{
    [Required] public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    [Required] public required Guid ApplicationId { get; set; }
    [StringLength(600)] public string? Note { get; set; }
}