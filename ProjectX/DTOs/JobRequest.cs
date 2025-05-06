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
    public ICollection<Guid>? ServiceIds { set; get; } = new List<Guid>();
    public bool IsDraft { set; get; } = false;
    public DateTime StartDate { set; get; } = DateTime.UtcNow;
    public DateTime EndDate { set; get; } = DateTime.UtcNow.AddDays(7);

    [Required] public Guid CampaignId { set; get; }

    [Required] public Guid LocationId { set; get; }

    public IFormFile? JobDescriptionFile { set; get; }

    public ICollection<Guid> Skills { get; set; } = new List<Guid>();

    public ICollection<Guid> ContractTypes { get; set; } = new List<Guid>();

    public ICollection<Guid> JobLevels { get; set; } = new List<Guid>();

    public ICollection<Guid> JobTypes { get; set; } = new List<Guid>();

    public JobPaymentMethod PaymentMethod { set; get; } = JobPaymentMethod.XToken;
    public PaymentGateway Gateway { set; get; } = PaymentGateway.VnPay;
}