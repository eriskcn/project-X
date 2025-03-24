using ProjectX.Models;

namespace ProjectX.DTOs;

public class CompanyRecruiterResponse
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = null!;
    public string? ShortName { get; set; }
    public string HeadQuarterAddress { get; set; } = null!;
    public string Logo { get; set; } = null!;
    public string ContactEmail { get; set; } = null!;
    public int FoundedYear { get; set; }
    public CompanySize Size { get; set; }
    public string Introduction { get; set; } = null!;
    public LocationResponse Location { get; set; } = null!;
    public MajorResponse Major { get; set; } = null!;
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}