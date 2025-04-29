using System.ComponentModel.DataAnnotations;

namespace ProjectX.DTOs;

public class RatingRequest
{
    [Range(0, 5)] public double Point { get; set; }
    public string? Comment { get; set; }
    public bool IsAnonymous { get; set; }
}