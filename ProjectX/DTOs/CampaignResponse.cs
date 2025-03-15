using ProjectX.Models;

namespace ProjectX.DTOs;

public class CampaignResponse
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public DateTime Open { get; set; }
    public DateTime Close { get; set; }
    public int CountJobs { get; set; }
    public CampaignStatus Status { get; set; }
}