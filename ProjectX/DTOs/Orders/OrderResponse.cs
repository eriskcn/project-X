using ProjectX.Models;

namespace ProjectX.DTOs.Orders;

public abstract class OrderResponse
{
    public Guid Id { get; set; }
    public double AmountCash { get; set; }
    public PaymentGateway Gateway { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}