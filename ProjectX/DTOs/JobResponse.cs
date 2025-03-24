using ProjectX.Models;

namespace ProjectX.DTOs;

public class JobResponse
{
    public Guid Id { set; get; }
    public required string Title { set; get; }
    public required string Description { set; get; }
    public int Quantity { set; get; }
    public required string OfficeAddress { set; get; }
    public JobStatus Status { set; get; }
    public EducationLevel? EducationLevelRequire { set; get; }
    public double? YearOfExperience { set; get; }
    public double? MinSalary { set; get; }
    public double? MaxSalary { set; get; }
    public bool IsHighlight { set; get; }
    public DateTime? HighlightStart { set; get; }
    public DateTime? HighlightEnd { set; get; }
    public int CountApplications { set; get; }
    public MajorResponse Major { set; get; } = null!;
    public LocationResponse Location { set; get; } = null!;
    public FileResponse? JobDescription { set; get; }
    public ICollection<SkillResponse> Skills { set; get; } = new List<SkillResponse>();
    public ICollection<ContractTypeResponse> ContractTypes { set; get; } = new List<ContractTypeResponse>();
    public ICollection<JobLevelResponse> JobLevels { set; get; } = new List<JobLevelResponse>();
    public ICollection<JobTypeResponse> JobTypes { set; get; } = new List<JobTypeResponse>();
    public DateTime Created { set; get; }
    public DateTime Modified { set; get; }
}