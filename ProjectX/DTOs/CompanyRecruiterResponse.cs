using ProjectX.Models;

namespace ProjectX.DTOs;

public class CompanyRecruiterResponse
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string HeadQuarterAddress { get; set; } = string.Empty;
    public string Logo { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public int FoundedYear { get; set; }
    public CompanySize Size { get; set; }
    public string Introduction { get; set; } = string.Empty;
    public LocationResponse Location { get; set; } = new LocationResponse();
    public MajorResponse Major { get; set; } = new MajorResponse();
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}