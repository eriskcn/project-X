using System.ComponentModel.DataAnnotations;
using ProjectX.Models;

namespace ProjectX.DTOs;

public class UpdateLocationRequest
{
    [Required] [StringLength(50)] public string? Name { get; set; }
    public Region? Region { set; get; }
}