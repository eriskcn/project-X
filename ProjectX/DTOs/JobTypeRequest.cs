using System.ComponentModel.DataAnnotations;

namespace ProjectX.DTOs;

public class JobTypeRequest
{
    [Required] [StringLength(50)] public required string Name { get; set; }
}