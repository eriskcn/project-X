using System.ComponentModel.DataAnnotations;
using ProjectX.Models;

namespace ProjectX.DTOs;

public class BusinessVerifyRequest
{
    [Required] [StringLength(450)] public required string CompanyName { get; set; }

    [Required] [StringLength(10)] public required string ShortName { get; set; }

    [Required] [StringLength(256)] public required string HeadQuarterAddress { get; set; }

    [Required] [StringLength(10)] public required string TaxCode { get; set; }

    [Required] public required IFormFile Logo { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(50)]
    public required string ContactEmail { get; set; }

    [Required] [Phone] [StringLength(10)] public required string ContactPhone { get; set; }

    [StringLength(256)] public string? Website { get; set; }

    [Required] [Range(1900, 2100)] public int FoundedYear { get; set; }

    public CompanySize Size { get; set; }

    [Required] [StringLength(10000)] public required string Introduction { get; set; }

    [Required] public required Guid LocationId { get; set; }

    [Required] public required ICollection<Guid> MajorIds { get; set; } = new List<Guid>();

    [Required] public required IFormFile RegistrationFile { get; set; }
}