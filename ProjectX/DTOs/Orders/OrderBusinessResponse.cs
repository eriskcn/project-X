using ProjectX.Controllers;

namespace ProjectX.DTOs.Orders;

public class OrderBusinessResponse : OrderResponse
{
    public PurchasedPackageResponse PurchasedPackage { get; set; } = null!;
}