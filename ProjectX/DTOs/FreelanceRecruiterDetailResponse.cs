using ProjectX.Models;

namespace ProjectX.DTOs;

public class FreelanceRecruiterDetailResponse
{
    public Guid Id { get; set; }
    public VerifyStatus Status { get; set; } 
    public string? RejectReason { get; set; }
    public FileResponse? FrontIdCard { get; set; }
    public FileResponse? BackIdCard { get; set; }
}