using System.ComponentModel.DataAnnotations;

namespace ProjectX.DTOs;

public class SkillRequest
{
    [Required] [StringLength(256)] public required string Name { get; set; }
}