using ProjectX.Data;

namespace ProjectX.Models;

public class Notification : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Content { get; set; }
}