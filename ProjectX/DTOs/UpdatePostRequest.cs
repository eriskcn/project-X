namespace ProjectX.DTOs;

public class UpdatePostRequest
{
    public string? Content { get; set; }
    public IFormFile? AttachedFile { get; set; }
}