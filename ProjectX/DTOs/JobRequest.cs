using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using ProjectX.Models;

namespace ProjectX.DTOs;

public class JobRequest
{
    [Required] [StringLength(100)] public required string Title { set; get; }

    [Required] [StringLength(10000)] public required string Description { set; get; }

    [Required] [StringLength(256)] public required string OfficeAddress { set; get; }

    [Required] [Range(1, int.MaxValue)] public int Quantity { set; get; } = 1;

    public EducationLevel? EducationLevelRequire { set; get; } = EducationLevel.University;

    [Range(0, double.MaxValue)] public double? YearOfExperience { set; get; }

    [Range(0, double.MaxValue)] public double? MinSalary { set; get; }

    [Range(0, double.MaxValue)] public double? MaxSalary { set; get; }

    [Required] public Guid MajorId { set; get; }

    public bool IsHighlight { set; get; }

    public DateTime? HighlightStart { set; get; }

    public DateTime? HighlightEnd { set; get; }

    [Required] public Guid CampaignId { set; get; }

    [Required] public Guid LocationId { set; get; }

    public IFormFile? JobDescriptionFile { set; get; }

    [FromForm(Name = "Skills[]")]
    [MinLength(1, ErrorMessage = "At least one skill is required.")]
    public ICollection<Guid> Skills { get; set; } = new List<Guid>();

    [FromForm(Name = "ContractTypes[]")]
    [MinLength(1, ErrorMessage = "At least one contract type is required.")]
    public ICollection<Guid> ContractTypes { get; set; } = new List<Guid>();

    [FromForm(Name = "JobLevels[]")]
    [MinLength(1, ErrorMessage = "At least one job level is required.")]
    public ICollection<Guid> JobLevels { get; set; } = new List<Guid>();

    [FromForm(Name = "JobTypes[]")]
    [MinLength(1, ErrorMessage = "At least one job type is required.")]
    public ICollection<Guid> JobTypes { get; set; } = new List<Guid>();
}