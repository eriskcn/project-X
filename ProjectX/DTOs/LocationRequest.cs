using ProjectX.Models;

namespace ProjectX.DTOs;

public class LocationRequest
{
    public required string Name { get; set; }
    public Region Region { get; set; }
}