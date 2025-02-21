using ProjectX.Models;

namespace ProjectX.DTOs;

public class UpdateLocationRequest
{
    public string? Name { set; get; }
    public Region? Region { set; get; }
}