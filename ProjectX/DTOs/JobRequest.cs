using ProjectX.Models;

namespace ProjectX.DTOs;

public class JobRequest
{
    public required string Title { set; get; }
    public required string Description { set; get; }
    public required string OfficeAddress { set; get; }
    public int Quantity { set; get; } = 1;
    public EducationLevel? Level { set; get; } = EducationLevel.University;
    public double? YearOfExperience { set; get; }
    public double? MinSalary { set; get; }
    public double? MaxSalary { set; get; }
    
    public Guid MajorId { set; get; }
    
    
    public byte IsHighlight { set; get; } = 0;
    
    public DateTime? HighlightStart { set; get; }
    
    public DateTime? HighlightEnd { set; get; }
    public Guid CampaignId { set; get; }
    public Guid LocationId { set; get; }
    public IFormFile? JobDescriptionFile { set; get; }
    public ICollection<Guid> Skills { set; get; } = new List<Guid>();
    public ICollection<Guid> ContractTypes { set; get; } = new List<Guid>();
    public ICollection<Guid> JobLevels { set; get; } = new List<Guid>();
    public ICollection<Guid> JobTypes { set; get; } = new List<Guid>();
}