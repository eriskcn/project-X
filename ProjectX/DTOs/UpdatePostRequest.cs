using System.ComponentModel.DataAnnotations;

namespace ProjectX.DTOs;

public class UpdatePostRequest
{
    [StringLength(1000)] public string? Content { get; set; }
    public IFormFile? AttachedFile { get; set; }
}