using ProjectX.Models;

namespace ProjectX.DTOs;

public class CampaignRequest
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public DateTime Open { get; set; }
    public DateTime Close { get; set; }
    public CampaignStatus Status { get; set; }
}