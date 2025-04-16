using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.Models;
using System.Web;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/vn-pay")]
[Authorize]
public class VNPayController(ApplicationDbContext context, IConfiguration config) : ControllerBase
{
    [HttpPost("create-payment/{orderId:guid}")]
    public async Task<IActionResult> CreatePayment(Guid orderId)
    {
        var order = await context.Orders.FindAsync(orderId);
        if (order == null)
        {
            return NotFound(new { Message = "Order not found." });
        }

        if (order.Status != OrderStatus.Pending)
        {
            return BadRequest(new { Message = "Order is not in Pending status." });
        }

        var vnpUrl = config["VNPay:vnp_Url"];
        var vnpTmnCode = config["VNPay:vnp_TmnCode"];
        var vnpHashSecret = config["VNPay:vnp_HashSecret"];
        var vnpReturnUrl = config["VNPay:vnp_ReturnUrl"];

        if (string.IsNullOrEmpty(vnpUrl) || string.IsNullOrEmpty(vnpTmnCode) || string.IsNullOrEmpty(vnpHashSecret) ||
            string.IsNullOrEmpty(vnpReturnUrl))
        {
            return StatusCode(500, new { Message = "VNPay configuration is missing." });
        }

        var payment = new Payment
        {
            OrderId = order.Id,
            VnpTxnRef = DateTime.Now.Ticks.ToString(),
            VnpAmount = order.Amount * 100,
            Status = PaymentStatus.Pending,
            Created = DateTime.UtcNow,
            Modified = DateTime.UtcNow
        };

        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        var vnpParams = new Dictionary<string, string>
        {
            { "vnp_Version", "2.1.0" },
            { "vnp_Command", "pay" },
            { "vnp_TmnCode", vnpTmnCode },
            { "vnp_Amount", payment.VnpAmount.ToString() },
            { "vnp_CurrCode", "VND" },
            { "vnp_TxnRef", payment.VnpTxnRef },
            {
                "vnp_OrderInfo",
                $"Highlight job {order.JobId} from {order.StartDate:yyyy-MM-dd} to {order.EndDate:yyyy-MM-dd}"
            },
            { "vnp_Locale", "vn" },
            { "vnp_ReturnUrl", vnpReturnUrl },
            { "vnp_IpAddr", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1" },
            { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
            { "vnp_ExpireDate", DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss") }
        };

        var sortedParams = vnpParams.OrderBy(k => k.Key).ToDictionary(k => k.Key, k => k.Value);
        var signData = string.Join("&", sortedParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        var vnpSecureHash = HmacSha512(vnpHashSecret, signData);

        var queryString = string.Join("&", sortedParams.Select(kvp => $"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value)}"));
        queryString += $"&vnp_SecureHash={vnpSecureHash}";

        return Ok(new
        {
            PaymentUrl = $"{vnpUrl}?{queryString}",
            PaymentId = payment.Id,
            Message = "Payment created successfully."
        });
    }

    [AllowAnonymous]
    [HttpGet("call-back")]
    public async Task<IActionResult> CallBack()
    {
        var vnpHashSecret = config["VNPay:vnp_HashSecret"];
        if (string.IsNullOrEmpty(vnpHashSecret))
        {
            return StatusCode(500, new { Message = "VNPay HashSecret is not configured." });
        }

        var queryParams = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());
        var vnpSecureHash = queryParams.TryGetValue("vnp_SecureHash", out var value) ? value : string.Empty;
        queryParams.Remove("vnp_SecureHash");

        var sortedParams = queryParams.OrderBy(k => k.Key).ToDictionary(k => k.Key, k => k.Value);
        var signData = string.Join("&",
            sortedParams.Where(kvp => !string.IsNullOrEmpty(kvp.Value)).Select(kvp => $"{kvp.Key}={kvp.Value}"));
        var computedHash = HmacSha512(vnpHashSecret, signData);

        var vnpTxnRef = queryParams.GetValueOrDefault("vnp_TxnRef", "");
        if (string.IsNullOrEmpty(vnpTxnRef))
        {
            return BadRequest(new { Message = "Missing vnp_TxnRef." });
        }

        var payment = await context.Payments.SingleOrDefaultAsync(p => p.VnpTxnRef == vnpTxnRef);
        if (payment == null)
        {
            return BadRequest(new { Message = "Payment not found." });
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            return Ok(new
            {
                IsValid = computedHash.Equals(vnpSecureHash, StringComparison.InvariantCultureIgnoreCase),
                ResponseCode = payment.VnpResponseCode,
                Message = $"Payment already processed as {payment.Status}."
            });
        }

        payment.VnpResponseCode = queryParams.GetValueOrDefault("vnp_ResponseCode", "");
        payment.VnpTransactionStatus = queryParams.GetValueOrDefault("vnp_TransactionStatus", "");
        payment.VnpSecureHash = vnpSecureHash;
        payment.VnpPayDate = DateTime.TryParseExact(
            queryParams.GetValueOrDefault("vnp_PayDate", ""),
            "yyyyMMddHHmmss",
            null,
            System.Globalization.DateTimeStyles.None,
            out var payDate
        )
            ? payDate
            : null;
        payment.Status = payment.VnpResponseCode == "00" ? PaymentStatus.Completed : PaymentStatus.Failed;
        payment.Modified = DateTime.UtcNow;

        if (payment.Status == PaymentStatus.Completed)
        {
            var order = await context.Orders.Include(o => o.Job).SingleOrDefaultAsync(o => o.Id == payment.OrderId);
            if (order is { Status: OrderStatus.Pending })
            {
                order.Status = OrderStatus.Completed;
                order.Modified = DateTime.UtcNow;
                order.Job.IsHighlight = true;
                order.Job.HighlightStart = order.StartDate;
                order.Job.HighlightEnd = order.EndDate;
            }
        }

        await context.SaveChangesAsync();

        var isValid = computedHash.Equals(vnpSecureHash, StringComparison.InvariantCultureIgnoreCase);
        return Ok(new
        {
            IsValid = isValid,
            ResponseCode = payment.VnpResponseCode,
            Message = payment.Status == PaymentStatus.Completed
                ? "Thanh toán thành công"
                : $"Thanh toán thất bại: {payment.VnpResponseCode}"
        });
    }

    private static string HmacSha512(string key, string inputData)
    {
        var hash = new StringBuilder();
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(inputData);
        using (var hmac = new HMACSHA512(keyBytes))
        {
            var hashValue = hmac.ComputeHash(inputBytes);
            foreach (var theByte in hashValue)
            {
                hash.Append(theByte.ToString("x2"));
            }
        }

        return hash.ToString();
    }
}