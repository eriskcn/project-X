using Microsoft.AspNetCore.Mvc;
using ProjectX.Models;

namespace ProjectX.DTOs;

public class JobRequest
{
    public required string Title { set; get; }
    public required string Description { set; get; }
    public required string OfficeAddress { set; get; }
    public int Quantity { set; get; } = 1;
    public EducationLevel? EducationLevelRequire { set; get; } = EducationLevel.University;
    public double? YearOfExperience { set; get; }
    public double? MinSalary { set; get; }
    public double? MaxSalary { set; get; }

    public Guid MajorId { set; get; }

    public bool IsHighlight { set; get; }

    public DateTime? HighlightStart { set; get; }

    public DateTime? HighlightEnd { set; get; }
    public Guid CampaignId { set; get; }
    public Guid LocationId { set; get; }
    public IFormFile? JobDescriptionFile { set; get; }
    
    [FromForm(Name = "Skills[]")] public ICollection<Guid> Skills { get; set; } = new List<Guid>();

    [FromForm(Name = "ContractTypes[]")] public ICollection<Guid> ContractTypes { get; set; } = new List<Guid>();

    [FromForm(Name = "JobLevels[]")] public ICollection<Guid> JobLevels { get; set; } = new List<Guid>();

    [FromForm(Name = "JobTypes[]")] public ICollection<Guid> JobTypes { get; set; } = new List<Guid>();
}