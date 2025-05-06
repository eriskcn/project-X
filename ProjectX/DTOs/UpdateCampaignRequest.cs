using System.ComponentModel.DataAnnotations;
using ProjectX.Models;

namespace ProjectX.DTOs;

public class UpdateCampaignRequest
{
    [StringLength(100)] public string? Name { get; set; }
    [StringLength(10000)] public string? Description { get; set; }

    // public DateTime? Open { get; set; }
    // public DateTime? Close { get; set; }
    public CampaignStatus? Status { get; set; }
}