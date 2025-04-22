namespace ProjectX.DTOs;

public class SavedJobResponse
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public double? MinSalary { set; get; }
    public double? MaxSalary { set; get; }
    public double? YearOfExperience { set; get; }
    public LocationResponse Location { get; set; } = null!;
    public UserResponse Recruiter { get; set; } = null!;
    public DateTime Created { get; set; }
}