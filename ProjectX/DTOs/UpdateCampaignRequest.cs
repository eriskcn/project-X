using ProjectX.Models;

namespace ProjectX.DTOs;

public class UpdateCampaignRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTime? Open { get; set; }
    public DateTime? Close { get; set; }
    public CampaignStatus? Status { get; set; }
}