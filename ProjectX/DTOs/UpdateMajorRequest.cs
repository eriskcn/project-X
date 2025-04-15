using System.ComponentModel.DataAnnotations;

namespace ProjectX.DTOs;

public class UpdateMajorRequest
{
    [Required] [StringLength(50)] public required string Name { get; set; }
}