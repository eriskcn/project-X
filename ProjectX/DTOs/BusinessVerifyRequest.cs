using ProjectX.Models;

namespace ProjectX.DTOs;

public class BusinessVerifyRequest
{
    public required string Name { get; set; }
    public string? ShortName { get; set; }
    public required string HeadQuarterAddress { get; set; }
    public required IFormFile Logo { get; set; }
    public required string ContactEmail { get; set; }
    public required int FoundedYear { get; set; }
    public CompanySize Size { get; set; }
    public required string Introduction { get; set; }
    public required Guid LocationId { get; set; }
    public required Guid MajorId { get; set; }
    public required IFormFile RegistrationFile { get; set; }
}