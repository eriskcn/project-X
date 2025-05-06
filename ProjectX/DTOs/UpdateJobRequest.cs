using System.ComponentModel.DataAnnotations;
using ProjectX.Models;

namespace ProjectX.DTOs;

public class UpdateJobRequest
{
    [StringLength(100)] public string? Title { set; get; }
    [StringLength(10000)] public string? Description { set; get; }
    [StringLength(256)] public string? OfficeAddress { set; get; }
    [Range(1, int.MaxValue)] public int? Quantity { set; get; }
    public EducationLevel? EducationLevelRequire { set; get; }
    [Range(0, double.MaxValue)] public double? YearOfExperience { set; get; }
    [Range(0, double.MaxValue)] public double? MinSalary { set; get; }
    [Range(0, double.MaxValue)] public double? MaxSalary { set; get; }

    public Guid MajorId { set; get; }
    public Guid LocationId { set; get; }
    public IFormFile? JobDescriptionFile { set; get; }

    public ICollection<Guid> Skills { set; get; } = new List<Guid>();

    public ICollection<Guid> ContractTypes { set; get; } = new List<Guid>();

    public ICollection<Guid> JobLevels { set; get; } = new List<Guid>();

    public ICollection<Guid> JobTypes { set; get; } = new List<Guid>();

    public JobStatus? Status { set; get; }
}