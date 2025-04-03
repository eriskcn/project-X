using ProjectX.Models;

namespace ProjectX.DTOs;

public class CompanyDetailResponse
{
    public Guid Id { get; set; }
    public required string CompanyName { get; set; }
    public string? ShortName { get; set; }
    public required string TaxCode { get; set; }
    public required string HeadQuarterAddress { get; set; }
    public required string Logo { get; set; }
    public required string ContactEmail { get; set; }
    public required string ContactPhone { get; set; }
    public string? Website { get; set; }
    public required int FoundedYear { get; set; }
    public CompanySize Size { get; set; }
    public required string Introduction { get; set; }
    public LocationResponse Location { get; set; } = null!;
    public MajorResponse Major { get; set; } = null!;
    public FileResponse? RegistrationFile { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}