namespace ProjectX.DTOs;

public class LikeResponse
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public bool IsLike { get; set; }
}