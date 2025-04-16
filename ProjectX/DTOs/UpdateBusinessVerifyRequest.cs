using System.ComponentModel.DataAnnotations;
using ProjectX.Models;

namespace ProjectX.DTOs;

public class UpdateBusinessVerifyRequest
{
    [StringLength(450)] public string? CompanyName { get; set; }

    [StringLength(10)] public string? ShortName { get; set; }

    [StringLength(256)] public string? HeadQuarterAddress { get; set; }

    [StringLength(10)] public string? TaxCode { get; set; }

    public IFormFile? Logo { get; set; }

    [EmailAddress] [StringLength(50)] public string? ContactEmail { get; set; }

    [Phone] [StringLength(10)] public string? ContactPhone { get; set; }

    [StringLength(256)] public string? Website { get; set; }

    [Range(1900, 2100)] public int? FoundedYear { get; set; }

    public CompanySize? Size { get; set; }

    [StringLength(10000)] public string? Introduction { get; set; }

    public Guid? LocationId { get; set; }

    public ICollection<Guid>? MajorIds { get; set; } = new List<Guid>();

    public IFormFile? RegistrationFile { get; set; }
}