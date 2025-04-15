using System.ComponentModel.DataAnnotations;

namespace ProjectX.DTOs;

public class UpdateSkillRequest
{
    [Required] [StringLength(50)] public required string Name { get; set; }
}