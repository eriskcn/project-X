using ProjectX.Models;

namespace ProjectX.DTOs;

public class CompanyRecruiterResponse
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string TaxCode { get; set; } = string.Empty;
    public string HeadQuarterAddress { get; set; } = string.Empty;
    public string Logo { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string? Website { get; set; }
    public int FoundedYear { get; set; }
    public CompanySize Size { get; set; }
    public string Introduction { get; set; } = string.Empty;
    public LocationResponse Location { get; set; } = new LocationResponse();
    public ICollection<MajorResponse> Majors { get; set; } = new List<MajorResponse>();
}