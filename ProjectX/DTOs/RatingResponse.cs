namespace ProjectX.DTOs;

public class RatingResponse
{
    public Guid Id { get; set; }
    public UserResponse Candidate { get; set; } = null!;
    public string? Comment { get; set; }
    public double Point { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
}