using System.ComponentModel.DataAnnotations;
using ProjectX.Models;

namespace ProjectX.DTOs;

public class CampaignRequest
{
    [Required] [StringLength(100)] public required string Name { get; set; }
    [Required] [StringLength(10000)] public required string Description { get; set; }
    [Required] public required CampaignStatus Status { get; set; } = CampaignStatus.Opened;
}