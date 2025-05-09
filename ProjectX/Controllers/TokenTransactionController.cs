using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectX.Data;
using ProjectX.Models;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/x-token")]
public class TokenTransactionController(ApplicationDbContext context) : ControllerBase
{
    private const double ExchangeRateCashToToken = 2_000;

    [HttpPost("top-up")]
    [Authorize(Policy = "EmailConfirmed")]
    public async Task<IActionResult> TopUpToken([FromBody] TopUpRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request.AmountCash % 10_000 != 0)
        {
            return BadRequest(new { Message = "AmountCash must be a multiple of 10,000." });
        }


        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized("Invalid user ID.");
        }

        var user = await context.Users.FindAsync(userGuid);
        if (user == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        var topUpTransaction = new TokenTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userGuid,
            Type = TokenTransactionType.TopUp,
            AmountToken = (int)(request.AmountCash / ExchangeRateCashToToken),
            Created = DateTime.UtcNow,
            Modified = DateTime.UtcNow
        };

        context.TokenTransactions.Add(topUpTransaction);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userGuid,
            Amount = request.AmountCash,
            Gateway = request.Gateway,
            TargetId = topUpTransaction.Id,
            Type = OrderType.TopUp,
            Created = DateTime.UtcNow,
            Modified = DateTime.UtcNow
        };

        context.Orders.Add(order);

        await context.SaveChangesAsync();

        return Ok(new { order.Id });
    }
}

public class TopUpRequest
{
    [Range(10_000, double.MaxValue)] public double AmountCash { get; set; }
    public PaymentGateway Gateway { get; set; }
}