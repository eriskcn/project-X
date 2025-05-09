using System.ComponentModel.DataAnnotations;
using ProjectX.Models;

namespace ProjectX.DTOs;

public class ServiceResponse
{
    public Guid Id { get; set; }
    [StringLength(150)] public required string Name { get; set; }
    [StringLength(500)] public required string Description { get; set; }
    public required int DayLimit { get; set; }
    public ServiceType Type { get; set; }
    public double CashPrice { get; set; }
    public int XTokenPrice { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}