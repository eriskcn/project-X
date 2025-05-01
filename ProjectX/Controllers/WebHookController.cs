using Microsoft.AspNetCore.Mvc;
using ProjectX.Data;
using ProjectX.DTOs;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/webhook")]
public class WebHookController(ApplicationDbContext context, ILogger<WebHookController> logger)
    : ControllerBase
{
    private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly ILogger<WebHookController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    [HttpPost("se-pay")]
    public async Task<IActionResult> HandleSePayWebhook([FromBody] SePayWebhookRequest request)
    {
        try
        {
            _logger.LogInformation("Received SePay webhook: {TransactionId}, Amount: {Amount}, Gateway: {Gateway}",
                request.Id, request.TransferAmount, request.Gateway);

            // Validate request
            if (string.IsNullOrEmpty(request.Gateway) || request.Id == 0)
            {
                _logger.LogWarning("Invalid webhook data received: {Request}", request);
                return BadRequest(new { success = false, message = "Invalid webhook data" });
            }


            _logger.LogInformation("Successfully processed webhook: {TransactionId}", request.Id);

            return Ok(new { success = true, message = "Webhook processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SePay webhook: {TransactionId}", request.Id);
            return StatusCode(500, new { success = false, message = "Error processing webhook" });
        }
    }

    [HttpPost("create-payment-qr/{orderId:guid}")]
    public async Task<IActionResult> CreatePaymentQr(Guid orderId)
    {
        return Ok();
    }
}