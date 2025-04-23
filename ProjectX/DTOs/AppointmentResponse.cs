namespace ProjectX.DTOs;

public class AppointmentResponse
{
    public Guid Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public ApplicationResponseForAppointment Application { get; set; } = null!;
    public UserResponse Participant { get; set; } = null!;
    public string? Note { get; set; }
    public DateTime Created { get; set; }
}