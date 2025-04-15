using System.ComponentModel.DataAnnotations;
using ProjectX.Models;

namespace ProjectX.DTOs;

public class PostRequest
{
    [Required] [StringLength(1000)] public required string Content { get; set; }
    public IFormFile? AttachedFile { get; set; }
}