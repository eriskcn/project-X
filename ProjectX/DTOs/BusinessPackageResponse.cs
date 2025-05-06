using System.ComponentModel.DataAnnotations;
using ProjectX.Models;

namespace ProjectX.DTOs;

public class BusinessPackageResponse
{
    public Guid Id { get; set; }
    [StringLength(256)] public required string Name { get; set; }
    [StringLength(256)] public required string Description { get; set; }
    public BusinessLevel Level { get; set; }
    public double CashPrice { get; set; }
    public int DurationInDays { get; set; }
    public int MonthlyXTokenRewards { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}