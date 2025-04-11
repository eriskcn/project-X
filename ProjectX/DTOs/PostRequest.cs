using ProjectX.Models;

namespace ProjectX.DTOs;

public class PostRequest
{
    public required string Content { get; set; }
    public IFormFile? AttachedFile { get; set; }
}