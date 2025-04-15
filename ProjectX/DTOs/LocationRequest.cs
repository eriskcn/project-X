using System.ComponentModel.DataAnnotations;
using ProjectX.Models;

namespace ProjectX.DTOs;

public class LocationRequest
{
    [Required] [StringLength(50)] public required string Name { get; set; }
    public Region Region { get; set; }
}