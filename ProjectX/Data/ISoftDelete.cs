namespace ProjectX.Data;

public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTime? Deleted { get; set; }
}