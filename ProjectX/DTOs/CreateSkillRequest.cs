namespace ProjectX.DTOs;

public class CreateSkillRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
}