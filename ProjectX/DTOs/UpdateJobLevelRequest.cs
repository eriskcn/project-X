using System.ComponentModel.DataAnnotations;

namespace ProjectX.DTOs;

public class UpdateJobLevelRequest
{
    [Required] [StringLength(50)] public required string Name { get; set; }
}