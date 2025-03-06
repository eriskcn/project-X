using ProjectX.Models;

namespace ProjectX.DTOs;

public class UpdateJobRequest
{
    public string? Title { set; get; }
    public string? Description { set; get; }
    public string? OfficeAddress { set; get; }
    public int? Quantity { set; get; } 
    public EducationLevel? Level { set; get; } 
    public double? YearOfExperience { set; get; }
    public double? MinSalary { set; get; }
    public double? MaxSalary { set; get; }
    public Guid MajorId { set; get; }
    public Guid CampaignId { set; get; }
    public Guid LocationId { set; get; }
    public IFormFile? JobDescriptionFile { set; get; }
    public ICollection<Guid> Skills { set; get; } = new List<Guid>();
    public ICollection<Guid> ContractTypes { set; get; } = new List<Guid>();
    public ICollection<Guid> JobLevels { set; get; } = new List<Guid>();
    public ICollection<Guid> JobTypes { set; get; } = new List<Guid>();
}