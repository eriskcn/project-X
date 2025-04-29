namespace ProjectX.DTOs;

public class CompanyProfileRequest
{
    public string? HeadQuarterAddress { get; set; }
    public IFormFile? Logo { get; set; }
    public IFormFile? Cover { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Website { get; set; }
    public string? Introduction { get; set; }
    public Guid? LocationId { get; set; }
    public ICollection<Guid> MajorIds { get; set; } = new List<Guid>();
}