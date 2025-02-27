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
    public EducationLevel? Level { set; get; }
    public double? YearOfExperience { set; get; }
    public double? MinSalary { set; get; }
    public double? MaxSalary { set; get; }
    public Major Major { set; get; } = null!;
    // public Guid CampaignId { set; get; }
    public Location Location { set; get; } = null!;
    public Guid? JobDescriptionId { set; get; }
    public ICollection<Skill> Skills { set; get; } = new List<Skill>();
    public ICollection<ContractType> ContractTypes { set; get; } = new List<ContractType>();
    public ICollection<JobLevel> JobLevels { set; get; } = new List<JobLevel>();
    public ICollection<JobType> JobTypes { set; get; } = new List<JobType>();
    public ICollection<Application> Applications { set; get; } = new List<Application>();
    public DateTime Created { set; get; }
    public DateTime Modified { set; get; }
}