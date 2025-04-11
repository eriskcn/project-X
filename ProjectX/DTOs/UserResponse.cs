namespace ProjectX.DTOs;

public class UserResponse
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string ProfilePicture { get; set; }
}