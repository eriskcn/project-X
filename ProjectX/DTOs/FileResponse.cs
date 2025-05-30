namespace ProjectX.DTOs;

public class FileResponse
{
    public Guid Id { get; set; }
    public Guid TargetId { get; set; }
    public required string Name { get; set; }
    public required string Path { get; set; }
    public DateTime Uploaded { get; set; }
}