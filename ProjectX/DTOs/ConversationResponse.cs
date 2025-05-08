namespace ProjectX.DTOs;

public class ConversationResponse
{
    public Guid Id { get; set; }
    public bool IsGroup { get; set; }
    public string? GroupName { get; set; }
    public string? GroupPicture { get; set; } 
    public UserResponse Participant { get; set; } = null!;
    public bool IsStored { get; set; }
    public DateTime LatestMessage { get; set; }
    public MessageResponse? LatestMessageDetails { get; set; }
    public ICollection<MessageResponse> Messages { get; set; } = new List<MessageResponse>();
}