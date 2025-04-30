using ProjectX.Models;

namespace ProjectX.DTOs;

public class ApplicationResponse
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string PhoneNumber { get; set; }
    public string? Introduction { get; set; }
    public Guid JobId { get; set; }
    public required FileResponse? Resume { get; set; }
    public ApplicationStatus Status { get; set; }
    public ApplicationProcess Process { get; set; }
    public AppointmentShortResponse? Appointment { get; set; }
    public DateTime? Submitted { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}