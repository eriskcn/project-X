namespace ProjectX.Data;

public class BaseEntity : ISoftDelete
{
    public bool IsDeleted { get; set; }
    public DateTime? Deleted { get; set; }
}