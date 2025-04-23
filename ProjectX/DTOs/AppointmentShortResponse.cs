namespace ProjectX.DTOs;

public class AppointmentShortResponse
{
    public Guid Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public UserResponse Participant { get; set; } = null!;
    public string? Note { get; set; }
    public DateTime Created { get; set; }
}