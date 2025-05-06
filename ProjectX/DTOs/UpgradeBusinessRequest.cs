using ProjectX.Models;

namespace ProjectX.DTOs;

public class UpgradeBusinessRequest
{
    public Guid PackageId { get; set; }
    public PaymentGateway Gateway { get; set; } = PaymentGateway.VnPay;
}