using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Helpers;
using ProjectX.Models;
using ProjectX.Services.QR;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/payments/se-pay")]
public class WebHookController(
    ApplicationDbContext context,
    ILogger<WebHookController> logger,
    IVietQrService qrService)
    : ControllerBase
{
    private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly ILogger<WebHookController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    [HttpPost("create-payment-qr")]
    [Authorize]
    public async Task<IActionResult> CreatePaymentQr([FromBody] OrderRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized(new { Message = "Invalid user ID." });
        }

        var user = await _context.Users.FindAsync(userGuid);
        if (user == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        var order = await _context.Orders.FindAsync(request.OrderId);
        if (order == null)
        {
            return NotFound(new { Message = "Order not found." });
        }

        if (order.Gateway != PaymentGateway.SePay)
        {
            return BadRequest(new { Message = "Invalid payment gateway" });
        }

        if (order.UserId != userGuid)
        {
            return Unauthorized(new { Message = "You are not authorized to pay this order." });
        }

        try
        {
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                PaymentGateway = order.Gateway,
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            var qrCode = await qrService.GenerateQuickLink(payment);
            return Ok(new
            {
                Message = "Create payment QR code successfully.",
                PaymentQRCode = qrCode
            });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> HandleSePayWebhook([FromBody] SePayWebhookRequest request)
    {
        try
        {
            var paymentId = PaymentHelper.ExtractGuid(request.Content);
            if (paymentId == null)
            {
                return BadRequest(new { Message = "Invalid payment Id" });
            }

            var payment = await _context.Payments
                .Include(p => p.Order)
                .SingleOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
            {
                return NotFound(new { Message = "Payment not found." });
            }

            payment.Gateway = request.Gateway;
            payment.TransactionRef = request.Id.ToString();
            payment.TransferType = request.TransferType;
            payment.AmountIn = request.TransferAmount;
            payment.AccountNumber = request.AccountNumber;
            payment.SubAccount = request.SubAccount;
            payment.Code = request.Code;
            payment.TransactionContent = request.Content;
            payment.WebhookTransactionId = request.Id;
            payment.TransactionDate = DateTime
                .SpecifyKind(DateTime.ParseExact(request.TransactionDate, "yyyy-MM-dd HH:mm:ss", null),
                    DateTimeKind.Local).ToUniversalTime();
            payment.Modified = DateTime.UtcNow;
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();

            var order = payment.Order;
            switch (order.Type)
            {
                case OrderType.TopUp:
                    var topUpTransaction = await _context.TokenTransactions.FindAsync(order.TargetId);
                    if (topUpTransaction == null)
                    {
                        return NotFound(new { Message = "Token transaction not found!" });
                    }

                    var user = await _context.Users.FindAsync(order.UserId);
                    if (user == null)
                    {
                        return NotFound(new { Message = "User not found." });
                    }

                    user.XTokenBalance += topUpTransaction.AmountToken;
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                    break;
                case OrderType.Job:
                    var job = await context.Jobs
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
                        }

                        jobService.IsActive = true;
                    }

                    context.Jobs.Update(job);
                    await context.SaveChangesAsync();
                    break;
                case OrderType.Business:
                    break;
            }

            _logger.LogInformation(
                "Received SePay webhook: {TransactionId}, Amount: {Amount}, Gateway: {Gateway}, PaymentId = {PaymentId}",
                request.Id, request.TransferAmount, request.Gateway, paymentId);

            if (string.IsNullOrEmpty(request.Gateway) || request.Id == 0)
            {
                _logger.LogWarning("Invalid webhook data received: {Request}", request);
                return BadRequest(new { success = false, message = "Invalid webhook data" });
            }


            _logger.LogInformation("Successfully processed webhook: {TransactionId}", request.Id);

            return Ok(new { success = true, message = "Webhook processed successfully" });
        }
        catch
            (Exception ex)
        {
            _logger.LogError(ex, "Error processing SePay webhook: {TransactionId}", request.Id);
            return StatusCode(500, new { success = false, message = "Error processing webhook" });
        }
    }
}