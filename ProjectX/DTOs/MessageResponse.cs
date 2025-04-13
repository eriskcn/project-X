namespace ProjectX.DTOs;

public class MessageResponse
{
    public Guid Id { get; set; }
    public string? Content { get; set; }
    public UserResponse Sender { get; set; } = null!;
    public UserResponse Receiver { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime? Read { get; set; }
    public bool IsEdited { get; set; }
    public DateTime? Edited { get; set; }
    public FileResponse? AttachedFile { get; set; }
    public DateTime Created { get; set; }
}