using ProjectX.Models;

namespace ProjectX.DTOs;

public class BusinessVerificationResponse
{
    public Guid UserId { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string PhoneNumber { get; set; }
    public required CompanyDetailResponse Company { get; set; }
    public bool BusinessVerified { get; set; }
}