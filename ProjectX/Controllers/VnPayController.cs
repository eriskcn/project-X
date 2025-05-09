using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.Helpers;
using ProjectX.Models;
using VNPAY.NET;
using VNPAY.NET.Enums;
using VNPAY.NET.Models;
using VNPAY.NET.Utilities;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/payments/vn-pay")]
public class VnPayController : ControllerBase
{
    private readonly IVnpay _vnPay;
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _dbContext;

    public VnPayController(IVnpay vnPay, IConfiguration configuration, ApplicationDbContext dbContext)
    {
        _vnPay = vnPay;
        _configuration = configuration;
        _dbContext = dbContext;

        _vnPay.Initialize(_configuration["VnPay:TmnCode"], _configuration["VnPay:HashSecret"],
            _configuration["VnPay:BaseUrl"], _configuration["VnPay:ReturnUrl"]);
    }

    [HttpGet("call-back")]
    public async Task<ActionResult<PaymentResult>> CallBack()
    {
        if (!Request.QueryString.HasValue)
        {
            return NotFound(new { Message = "Payment not found." });
        }

        try
        {
            var paymentResult = _vnPay.GetPaymentResult(Request.Query);
            var paymentGuid = PaymentHelper.ExtractGuid(paymentResult.Description);
            if (paymentGuid == null)
            {
                return NotFound(new { Message = "Payment Id not found." });
            }

            var payment = await _dbContext.Payments.FindAsync(paymentGuid);

            if (payment == null)
            {
                return NotFound(new { Message = "Payment not found." });
            }

            if (payment.Status == PaymentStatus.Completed)
            {
                return Ok(new { Message = "Payment already processed." });
            }

            payment.VnpTransactionStatus = paymentResult.TransactionStatus.Code.ToString().Replace("Code_", "");
            payment.VnpResponseCode = paymentResult.PaymentResponse.Code.ToString().Replace("Code_", "");
            payment.VnpPayDate = paymentResult.Timestamp;
            payment.Status = paymentResult.IsSuccess ? PaymentStatus.Completed : PaymentStatus.Failed;
            payment.Modified = DateTime.UtcNow;

            _dbContext.Payments.Update(payment);
            await _dbContext.SaveChangesAsync();

            if (paymentResult.IsSuccess)
            {
                var orderGuid = payment.OrderId;

                var order = await _dbContext.Orders.FindAsync(orderGuid);
                if (order == null)
                {
                    return NotFound(new { Message = "Order not found." });
                }

                order.Status = OrderStatus.Completed;
                switch (order.Type)
                {
                    case OrderType.TopUp:
                        var topUpTransaction = await _dbContext.TokenTransactions.FindAsync(order.TargetId);
                        if (topUpTransaction == null)
                        {
                            return NotFound(new { Message = "Top up transaction not found." });
                        }

                        var user = await _dbContext.Users.FindAsync(order.UserId);
                        if (user == null)
                        {
                            return NotFound(new { Message = "User not found." });
                        }

                        user.XTokenBalance += topUpTransaction.AmountToken;
                        _dbContext.Users.Update(user);
                        break;
                    case OrderType.Job:
                        var job = await _dbContext.Jobs
                            .Include(j => j.JobServices)
                            .ThenInclude(js => js.Service)
                            .SingleOrDefaultAsync(j => j.Id == order.TargetId);
                        if (job == null)
                        {
                            return NotFound(new { Message = "Job not found." });
                        }

                        var jobServices = job.JobServices;
                        foreach (var jobService in jobServices)
                        {
                            switch (jobService.Service.Type)
                            {
                                case ServiceType.Highlight:
                                    job.IsHighlight = true;
                                    break;
                                case ServiceType.Hot:
                                    job.IsHot = true;
                                    break;
                                case ServiceType.Urgent:
                                    job.IsUrgent = true;
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }

                            jobService.IsActive = true;
                        }

                        _dbContext.Jobs.Update(job);
                        break;

                    case OrderType.Business:
                        var purchased = await _dbContext.PurchasedPackages
                            .Include(pp => pp.User)
                            .Include(pp => pp.BusinessPackage)
                            .SingleOrDefaultAsync(pp => pp.Id == order.TargetId);
                        if (purchased == null)
                        {
                            return NotFound(new { Message = "Purchased package not found." });
                        }

                        purchased.IsActive = true;
                        purchased.StartDate = DateTime.UtcNow;
                        purchased.NextResetDate = purchased.StartDate.AddDays(30);
                        purchased.EndDate = purchased.StartDate.AddDays(purchased.BusinessPackage.DurationInDays);
                        purchased.User.XTokenBalance += purchased.BusinessPackage.MonthlyXTokenRewards;
                        purchased.User.Level = purchased.BusinessPackage.Level == BusinessLevel.Elite
                            ? AccountLevel.Elite
                            : AccountLevel.Premium;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                await _dbContext.SaveChangesAsync();

                _dbContext.Orders.Update(order);
                await _dbContext.SaveChangesAsync();
                return Ok(paymentResult);
            }

            return BadRequest(paymentResult);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = $"Error processing callback: {ex.Message}" });
        }
    }

    [HttpPost("create-payment-url")]
    [Authorize(Policy = "EmailConfirmed")]
    public async Task<ActionResult<string>> CreatePaymentUrl([FromBody] OrderRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized("Invalid user ID.");
        }

        var user = await _dbContext.Users.FindAsync(userGuid);
        if (user == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        var order = await _dbContext.Orders.FindAsync(request.OrderId);
        if (order == null)
        {
            return NotFound(new { Message = "Order not found." });
        }

        if (order.Gateway != PaymentGateway.VnPay)
        {
            return BadRequest(new { Message = "Invalid payment gateway" });
        }

        if (user.Id != order.UserId)
        {
            return Unauthorized(new { Message = "You are not authorized to pay this order." });
        }

        try
        {
            var paymentId = Guid.NewGuid();
            var payment = new Payment
            {
                Id = paymentId,
                OrderId = order.Id,
                Gateway = "VNPay",
                PaymentGateway = PaymentGateway.VnPay,
                Status = PaymentStatus.Pending,
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            };

            _dbContext.Payments.Add(payment);
            await _dbContext.SaveChangesAsync();

            var ipAddress = SafeNetworkHelper.GetClientIp(HttpContext);
            var paymentRequest = new PaymentRequest
            {
                PaymentId = DateTime.Now.Ticks,
                Money = order.Amount,
                Description = $"PAY{paymentId:N}",
                IpAddress = ipAddress,
                BankCode = BankCode.ANY,
                CreatedDate = DateTime.UtcNow,
                Currency = Currency.VND,
                Language = DisplayLanguage.Vietnamese
            };

            var paymentUrl = _vnPay.GetPaymentUrl(paymentRequest);

            payment.VnpTxnRef = paymentRequest.PaymentId.ToString();
            payment.VnpAmount = paymentRequest.Money;
            _dbContext.Payments.Update(payment);
            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                Message = "Create payment URL successfully.",
                PaymentUrl = paymentUrl
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Hehe = ex.Message });
        }
    }
}

public class OrderRequest
{
    public Guid OrderId { set; get; }
}