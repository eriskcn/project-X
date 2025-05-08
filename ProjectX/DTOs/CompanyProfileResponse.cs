using ProjectX.Models;

namespace ProjectX.DTOs;

public class CompanyProfileResponse
{
    public Guid Id { get; set; }
    public required string CompanyName { get; set; }
    public required string ShortName { get; set; }
    public required string TaxCode { get; set; }
    public required string HeadQuarterAddress { get; set; }
    public required string Logo { get; set; }
    public string? Cover { get; set; } 
    public required string ContactEmail { get; set; }
    public required string ContactPhone { get; set; }
    public string? Website { get; set; }
    public required int FoundedYear { get; set; }
    public CompanySize Size { get; set; }
    public required string Introduction { get; set; }
    public required LocationResponse Location { get; set; }
    public required ICollection<MajorResponse> Majors { get; set; } = new List<MajorResponse>();
    public bool IsElite { get; set; } = false;
    public double AvgRatings { get; set; } = 0;
}