using ProjectX.Models;

namespace ProjectX.DTOs;

public class LocationResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Region? Region { get; set; } 
}