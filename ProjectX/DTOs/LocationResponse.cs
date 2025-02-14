using ProjectX.Models;

namespace ProjectX.DTOs;

public class LocationResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public Region Region { get; set; }
}