namespace ProjectX.DTOs;

public class MessageRequest
{
    public Guid ReceiverId { get; set; }
    public string? Content { get; set; }
    public IFormFile? AttachedFile { get; set; } 
}