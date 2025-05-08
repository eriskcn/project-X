using ProjectX.Controllers;

namespace ProjectX.DTOs.Orders;

public class OrderJobResponse : OrderResponse
{
    public ICollection<JobServiceResponse>? Services { get; set; } = new List<JobServiceResponse>();
}