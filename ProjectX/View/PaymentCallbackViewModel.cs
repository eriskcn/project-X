using ProjectX.Models;
using VNPAY.NET.Models;

namespace ProjectX.View;

public class PaymentCallbackViewModel
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; }
    public PaymentResult PaymentResult { get; set; }
}