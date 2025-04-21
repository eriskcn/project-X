namespace ProjectX.DTOs;

public class PostResponse
{
    public Guid Id { get; set; }
    public required string Content { get; set; }
    public bool? Liked { get; set; }
    public bool IsEdited { get; set; }
    public DateTime? Edited { get; set; }
    public UserResponse User { get; set; } = null!;
    public PostResponse? ParentPost { get; set; }
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
    public FileResponse? AttachedFile { get; set; }
    public ICollection<PostResponse>? Comments { get; set; } = new List<PostResponse>();
    public DateTime Created { get; set; }
}